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

        private bool PointContainsSnake(GameStatusRequest gameStatusRequest, Point pt)
        {
            return gameStatusRequest.Board.Snakes.Any(s => s.Body.Any(b => b.X == pt.X && b.Y == pt.Y));
        }

        /// <summary>
        /// This request will be sent for every turn of the game.
        /// Use the information provided to determine how your
        /// Battlesnake will move on that turn, either up, down, left, or right.
        /// </summary>
        [HttpPost("move")]
        public IActionResult Move(GameStatusRequest gameStatusRequest)
        {
            var direction = new List<string>(); // {"down", "left", "right", "up"};
            var curCoords = gameStatusRequest.You.Head;
            var upPoint = new Point(curCoords.X, curCoords.Y + 1);
            var downPoint = new Point(curCoords.X, curCoords.Y - 1);
            var leftPoint = new Point(curCoords.X - 1, curCoords.Y);
            var rightPoint = new Point(curCoords.X + 1, curCoords.Y);
            if (upPoint.Y < gameStatusRequest.Board.Height && !PointContainsSnake(gameStatusRequest, upPoint))
                direction.Add("up");
            if (downPoint.Y >= 0 && !PointContainsSnake(gameStatusRequest, downPoint))
                direction.Add("down");
            if (leftPoint.X >= 0 && !PointContainsSnake(gameStatusRequest, leftPoint))
                direction.Add("left");
            if (rightPoint.X < gameStatusRequest.Board.Width && !PointContainsSnake(gameStatusRequest, rightPoint))
                direction.Add("right");

            if(!direction.Any())
            {
             //   direction.Add("up"); // will run into something
            }

            var rng = new Random();

            var response = new MoveResponse
            {
                Move = direction[rng.Next(direction.Count)],
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