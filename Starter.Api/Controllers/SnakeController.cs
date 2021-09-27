using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Starter.Api.Requests;
using Starter.Api.Responses;
using Starter.Core;

namespace Starter.Api.Controllers
{
    [ApiController]
    public class SnakeController : ControllerBase
    {
        /// <summary>
        /// This request will be made periodically to retrieve information about your Battlesnake,
        /// including its display options, author, etc.
        /// </summary>
        [HttpGet("")]
        public IActionResult Index()
        {
            var response = new InitResponse
            {
                ApiVersion = "1",
                Author = "Jake",
                Color = "#f7c244",
                Head = "default",
                Tail = "default"
            };

            return Ok(response);
        }


        /// <summary>
        /// Your Battlesnake will receive this request when it has been entered into a new game.
        /// Every game has a unique ID that can be used to allocate resources or data you may need.
        /// Your response to this request will be ignored.
        /// </summary>
        [HttpPost("start")]
        public IActionResult Start(GameStatusRequest gameStatusRequest)
        {
            return Ok();
        }

        private List<Point> GetOpenNeighbors(GameStatusRequest gameStatusRequest, HashSet<Point> occupied, Point curCoords)
        {
            var neighbors = new List<Point>();
            var upPoint = new Point(curCoords.X, curCoords.Y + 1);
            var downPoint = new Point(curCoords.X, curCoords.Y - 1);
            var leftPoint = new Point(curCoords.X - 1, curCoords.Y);
            var rightPoint = new Point(curCoords.X + 1, curCoords.Y);
            if (upPoint.Y < gameStatusRequest.Board.Height && !occupied.Contains(upPoint))
                neighbors.Add(upPoint);
            if (downPoint.Y >= 0 && !occupied.Contains(downPoint))
                neighbors.Add(downPoint);
            if (leftPoint.X >= 0 && !occupied.Contains(leftPoint))
                neighbors.Add(leftPoint);
            if (rightPoint.X < gameStatusRequest.Board.Width && !occupied.Contains(rightPoint))
                neighbors.Add(rightPoint);

            return neighbors;
        }

        private int GetOpenSpace(GameStatusRequest gameStatusRequest, HashSet<Point> occupied, Point pointToTest)
        {
            var totalSpace = 0;
            var tempOccupied = occupied.ToHashSet();
            var toCheck = new Queue<Point>();
            toCheck.Enqueue(pointToTest);
            while (toCheck.Any())
            {
                var point = toCheck.Dequeue();
                if (tempOccupied.Contains(point))
                    continue;

                totalSpace++;
                tempOccupied.Add(point);

                var neighbors = GetOpenNeighbors(gameStatusRequest, occupied, point);
                foreach(var neighbor in neighbors)
                {
                    toCheck.Enqueue(neighbor);
                }
            }

            return totalSpace;
        }

        private List<Snake> GetPossibleHeadCollision(GameStatusRequest gameStatusRequest, Point point)
        {
            var neighbors = GetOpenNeighbors(gameStatusRequest, new HashSet<Point>(), point);
            return gameStatusRequest.Board.Snakes.Where(s => s.Id != gameStatusRequest.You.Id && neighbors.Contains(s.Head)).ToList();
        }

        /// <summary>
        /// This request will be sent for every turn of the game.
        /// Use the information provided to determine how your
        /// Battlesnake will move on that turn, either up, down, left, or right.
        /// </summary>
        [HttpPost("move")]
        public IActionResult Move(GameStatusRequest gameStatusRequest)
        {
            var occupied = gameStatusRequest.Board.Snakes.SelectMany(s => s.Body.Take(s.Body.Count() - 1)).ToHashSet();
            Console.WriteLine("Occupied: " + string.Join(' ', occupied.Select(o => o.X.ToString() + "," + o.Y).ToList()));
            var curCoords = gameStatusRequest.You.Head;
            var openNeighs = GetOpenNeighbors(gameStatusRequest, occupied, curCoords);
            var maxOpenSpace = 0;
            var maxOpenNeighbors = 0;
            var best = openNeighs.FirstOrDefault();
            foreach(var neighbor in openNeighs)
            {
                var possibleCollisions = GetPossibleHeadCollision(gameStatusRequest, neighbor);
                var openSpace = GetOpenSpace(gameStatusRequest, occupied, neighbor);
                if (possibleCollisions.Any())
                {
                    if(possibleCollisions.Any(s => s.Length >= gameStatusRequest.You.Length))
                        continue;

                    if(possibleCollisions.All(s => s.Length < gameStatusRequest.You.Length)
                       && openSpace > gameStatusRequest.You.Length)
                    {
                        best = neighbor;
                        break;
                    }
                }
               
                var openNeighbors = GetOpenNeighbors(gameStatusRequest, occupied, neighbor);

                if (openSpace > maxOpenSpace)
                {
                    maxOpenSpace = openSpace;
                    maxOpenNeighbors = openNeighbors.Count();
                    best = neighbor;
                }
                else if(openSpace == maxOpenSpace)
                {
                    // If there is enough room and there is food.
                    if (openSpace > gameStatusRequest.You.Body.Count() + 1 && gameStatusRequest.Board.Food.Any(f => f.X == neighbor.X && f.Y == neighbor.Y))
                    {
                        maxOpenSpace = openSpace;
                        best = neighbor;
                        break;
                    }

                    if (openNeighbors.Count() > maxOpenNeighbors)
                    {
                        best = neighbor;
                        maxOpenNeighbors = openNeighbors.Count();
                        maxOpenSpace = openSpace;
                    }
                }
            }
            var direction = "up"; // {"down", "left", "right", "up"};
            if (best.X > curCoords.X)
                direction = "right";
            else if (best.X < curCoords.X)
                direction = "left";
            else if (best.Y > curCoords.Y)
                direction = "up";
            else
                direction = "down";

            var response = new MoveResponse
            {
                Move = direction,
                Shout = "I am moving!"
            };
            return Ok(response);
        }


        /// <summary>
        /// Your Battlesnake will receive this request whenever a game it was playing has ended.
        /// Use it to learn how your Battlesnake won or lost and deallocated any server-side resources.
        /// Your response to this request will be ignored.
        /// </summary>
        [HttpPost("end")]
        public IActionResult End(GameStatusRequest gameStatusRequest)
        {
            return Ok();
        }
    }

    public class MoveCandidate
    {
        public string Direction { get; set; }
        public List<Snake> PossibleCollisions { get; set; } = new List<Snake>();
        public int OpenSpaces { get; set; }
        public List<Point> OpenNeighbors { get; set; } = new List<Point>();
    }
}
