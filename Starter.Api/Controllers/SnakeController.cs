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
                Head = "shades",
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
            var otherSnakes = gameStatusRequest.Board.Snakes
                .Where(s => s.Id != gameStatusRequest.You.Id).ToList();
            var openSpaces = GetOpenSpacesDict(gameStatusRequest, occupied, otherSnakes);
            var foodHS = gameStatusRequest.Board.Food.ToHashSet();
            var shortestDistToFood = int.MaxValue;

            foreach (var neighbor in openNeighs)
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
                        maxOpenSpace = openSpace;
                        break;
                    }
                }

                // Test to see if snake can trap other snakes in a small space.
                var testOccupied = occupied.ToHashSet();
                testOccupied.Add(neighbor);
                var openSpacesWithTest = GetOpenSpacesDict(gameStatusRequest, testOccupied, otherSnakes);
                var neighborsNeighbors = GetOpenNeighbors(gameStatusRequest, testOccupied, neighbor);
                var myOpenSpaceWithTest = neighborsNeighbors.Any() ? neighborsNeighbors.Max(n => GetOpenSpace(gameStatusRequest, testOccupied, n)) : 0;
                    
                // If going into this cell closes off a section for another snake, but doesn't close off 
                if (openSpacesWithTest.Any(s => s.Value.OpenSpace < openSpaces[s.Key].OpenSpace && s.Value.OpenSpace < s.Value.Snake.Length) && myOpenSpaceWithTest > gameStatusRequest.You.Length)
                {
                    maxOpenSpace = openSpace;
                    best = neighbor;
                    break;
                }

                var openNeighbors = GetOpenNeighbors(gameStatusRequest, occupied, neighbor);
                var distanceToClosestFood = GetDistanceToClosestRequestedPoints(gameStatusRequest, foodHS, occupied, neighbor, 6);

                if (openSpace > maxOpenSpace)
                {
                    maxOpenSpace = openSpace;
                    maxOpenNeighbors = openNeighbors.Count();
                    best = neighbor;
                    shortestDistToFood = distanceToClosestFood;
                }
                else if(openSpace == maxOpenSpace)
                {
                    // If there is enough room and there is food.
                    if (openSpace > gameStatusRequest.You.Body.Count() + 1 && distanceToClosestFood == 1)
                    {
                        maxOpenSpace = openSpace;
                        best = neighbor;
                        break;
                    }

                    //if (openNeighbors.Count() > maxOpenNeighbors)
                    if(distanceToClosestFood < shortestDistToFood)
                    {
                        best = neighbor;
                        maxOpenNeighbors = openNeighbors.Count();
                        shortestDistToFood = distanceToClosestFood;
                        maxOpenSpace = openSpace;
                    } else if(distanceToClosestFood == shortestDistToFood)
                    {
                        if(openNeighbors.Count() > maxOpenNeighbors)
                        {
                            best = neighbor;
                            maxOpenNeighbors = openNeighbors.Count();
                            shortestDistToFood = distanceToClosestFood;
                            maxOpenSpace = openSpace;
                        }
                    }
                }
            }

            if(maxOpenSpace < 2 * gameStatusRequest.You.Length)
            {
                Point firstOpening = null;
                var ind = 0;
                var tempOcc = occupied.ToHashSet();
                var maxSnakeLength = gameStatusRequest.Board.Snakes.Max(s => s.Length);
                while (firstOpening == null && ind < maxSnakeLength)
                {
                    firstOpening = gameStatusRequest.Board.Snakes.Select(s => s.Body.Skip(s.Body.Count() - ind).FirstOrDefault()).Where(s => s != null)
                        .FirstOrDefault(p =>
                        {
                            tempOcc.Remove(p);
                            var openSp = GetOpenSpace(gameStatusRequest, tempOcc, gameStatusRequest.You.Head);
                            return openSp > maxOpenSpace;
                        });
                    ind++;
                }

                if (firstOpening != null)
                {
                    Point farthestNeighborFromSoonestOpenPoint = null;
                    var farthestNeightborsDistanceFromOpenPoint = 0;
                    var pointHS = new HashSet<Point>();
                    pointHS.Add(firstOpening);
                    foreach (var neigh in openNeighs)
                    {
                        var distanceFromOpenPoint = GetDistanceToClosestRequestedPoints(gameStatusRequest, pointHS, tempOcc, neigh, 99);
                        if (distanceFromOpenPoint == int.MaxValue)
                            continue;
                        if (distanceFromOpenPoint > farthestNeightborsDistanceFromOpenPoint)
                        {
                            farthestNeightborsDistanceFromOpenPoint = distanceFromOpenPoint;
                            farthestNeighborFromSoonestOpenPoint = neigh;
                        }
                    }
                    if (farthestNeighborFromSoonestOpenPoint != null)
                        best = farthestNeighborFromSoonestOpenPoint;
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

        private int GetDistanceToClosestRequestedPoints(GameStatusRequest gameStatusRequest, HashSet<Point> requestedPoints, HashSet<Point> occupied, Point point, int maxDistanceToLook)
        {
            var tempOccupied = occupied.ToHashSet();
            var q = new Queue<Tuple<Point, int>>();
            q.Enqueue(new Tuple<Point, int>(point, 1));
            while (q.Any())
            {
                var pt = q.Dequeue();
                if (tempOccupied.Contains(pt.Item1))
                    continue;

                if (requestedPoints.Contains(pt.Item1))
                    return pt.Item2;

                tempOccupied.Add(pt.Item1);

                if (pt.Item2 > maxDistanceToLook)
                    continue;

                var openNeighbors = GetOpenNeighbors(gameStatusRequest, tempOccupied, pt.Item1);
                foreach(var neighbor in openNeighbors)
                {
                    q.Enqueue(new Tuple<Point, int>(neighbor, pt.Item2 + 1));
                }
            }
            return int.MaxValue;
        }

        private Dictionary<string, OpenSpaceLookup> GetOpenSpacesDict(GameStatusRequest gameStatusRequest, HashSet<Point> occupied, List<Snake> snakes)
        { 
            return snakes
                .ToDictionary(s => s.Id, s =>
                 {
                     var tempOpenSpaces = GetOpenNeighbors(gameStatusRequest, occupied, s.Head);
                     var max = tempOpenSpaces.Any() ? tempOpenSpaces.Max(z => GetOpenSpace(gameStatusRequest, occupied, z)) : 0;
                     var lookup = new OpenSpaceLookup { Snake = s, OpenSpace = max };
                     return lookup;
                 });
        }

        private class OpenSpaceLookup
        {
            public Snake Snake { get; set; }
            public int OpenSpace { get; set; }
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
