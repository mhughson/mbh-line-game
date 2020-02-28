using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static mbh_line_game.Game1;

namespace mbh_line_game
{
    class math_utils
    {
        static public bool intersects_obj_obj(sprite a, sprite b)
        {
            //return intersects_box_box(a.x,a.y,a.w,a.h,b.x,b.y,b.w,b.h)
            return intersects_box_box(
                a.cx, a.cy, a.cw * 0.5f, a.ch * 0.5f,
                b.cx, b.cy, b.cw * 0.5f, b.ch * 0.5f);
        }

        static public bool intersects_obj_box(sprite a, float x1, float y1, float w1, float h1)
        {
            return intersects_box_box(a.cx, a.cy, a.cw * 0.5f, a.ch * 0.5f, x1, y1, w1, h1);
        }

        static public bool intersects_point_obj(float px, float py, sprite b)
        {
            return intersects_point_box(px, py, b.cx, b.cy, b.cw * 0.5f, b.ch * 0.5f);
        }

        //point to box intersection.
        static public bool intersects_point_box(float px, float py, float x, float y, float w, float h)
        {
            if (inst.flr(px) >= inst.flr(x - (w)) && inst.flr(px) < inst.flr(x + (w)) && inst.flr(py) >= inst.flr(y - (h)) && inst.flr(py) < inst.flr(y + (h)))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        //box to box intersection
        static public bool intersects_box_box(float x1, float y1, float w1, float h1, float x2, float y2, float w2, float h2)
        {
            var xd = x1 - x2;

            var xs = w1 + w2;
            if (inst.abs(xd) >= xs)
            {
                return false;
            }

            var yd = y1 - y2;

            var ys = h1 + h2;

            if (inst.abs(yd) >= ys)
            {
                return false;
            }

            return true;
        }
    }
}
