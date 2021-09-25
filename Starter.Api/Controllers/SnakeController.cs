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

        /// <summary>
        /// This request will be sent for every turn of the game.
        /// Use the information provided to determine how your
        /// Battlesnake will move on that turn, either up, down, left, or right.
        /// </summary>
        [HttpPost("move")]
        public IActionResult Move(GameStatusRequest gameStatusRequest)
        {
            var occupied = gameStatusRequest.Board.Snakes.SelectMany(s => s.Body).ToHashSet();
            var direction = "up"; // {"down", "left", "right", "up"};
            var curCoords = gameStatusRequest.You.Head;
            var upPoint = new Point(curCoords.X, curCoords.Y + 1);
            var downPoint = new Point(curCoords.X, curCoords.Y - 1);
            var leftPoint = new Point(curCoords.X - 1, curCoords.Y);
            var rightPoint = new Point(curCoords.X + 1, curCoords.Y);
            var maxOpenNeighbors = 0;
            if (upPoint.Y < gameStatusRequest.Board.Height && !occupied.Contains(upPoint))
            {
                var openNeighbors = GetOpenNeighbors(gameStatusRequest, occupied, upPoint);
                if(openNeighbors.Count() > maxOpenNeighbors)
                { 
                    direction = "up";
                    maxOpenNeighbors = openNeighbors.Count();
                }
            }
            if (downPoint.Y >= 0 && !occupied.Contains(downPoint))
            {
                var openNeighbors = GetOpenNeighbors(gameStatusRequest, occupied, downPoint);
                if(openNeighbors.Count() > maxOpenNeighbors)
                {
                    direction = "down";
                    maxOpenNeighbors = openNeighbors.Count();
                }
            }
            if (leftPoint.X >= 0 && !occupied.Contains(leftPoint))
            {
                var openNeighbors = GetOpenNeighbors(gameStatusRequest, occupied, leftPoint);
                if(openNeighbors.Count() > maxOpenNeighbors)
                {
                    direction = "left";
                    maxOpenNeighbors = openNeighbors.Count();
                }
            }
            if (rightPoint.X < gameStatusRequest.Board.Width && !occupied.Contains(rightPoint))
            {
                var openNeighbors = GetOpenNeighbors(gameStatusRequest, occupied, rightPoint);
                if(openNeighbors.Count() > maxOpenNeighbors)
                {
                    direction = "right";
                    maxOpenNeighbors = openNeighbors.Count();
                }
            }

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
}
