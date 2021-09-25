using System;﻿

namespace Starter.Core
{
    public class Point
    {
        public int X { get; set; }
        public int Y { get; set; }
        
        public Point(int x, int y)
        {
            X = x;
            Y = y;
        }

   public override bool Equals(object obj)
   {
      //Check for null and compare run-time types.
      if ((obj == null) || ! this.GetType().Equals(obj.GetType()))
      {
         return false;
      }
      else {
         Point p = (Point) obj;
         return (X == p.X) && (Y == p.Y);
      }
   }

   public override int GetHashCode()
   {
      return (X << 2) ^ Y;
   }

    public override string ToString()
    {
        return String.Format("Point({0}, {1})", X, Y);
    }
    }
}
