using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static mbh_line_game.Game1;

namespace mbh_line_game
{
    public class hit_pause_manager : PicoXObj
    {
        public enum pause_reason
        {
            temp,
        }

        Dictionary<pause_reason, int> pause_times = new Dictionary<pause_reason, int>()
        {
            { pause_reason.temp, 0 },
        };

        public int pause_time_remaining { get; protected set; }

        public hit_pause_manager()
        {
            pause_time_remaining = 0;
        }

        public void start_pause(pause_reason reason)
        {
            pause_time_remaining = (int)inst.max(pause_time_remaining, pause_times[reason]);
        }

        public override void _update60()
        {
            base._update60();

            pause_time_remaining = (int)inst.max(0, pause_time_remaining - 1);
        }

        public bool is_paused()
        {
            return pause_time_remaining > 0;
        }
    }

    public class timer_callback : PicoXObj
    {
        int delay;
        Action callback;

        public timer_callback(int delay, Action callback)
        {
            this.delay = delay;
            this.callback = callback;

        }

        public override void _update60()
        {
            base._update60();

            delay--;

            if (delay <= 0)
            {
                callback();
                inst.objs_remove_queue.Add(this);
            }
        }
    }

    public class game_utils
    {
        static public void printo(string str, float startx, float starty, int col, int col_bg)
        {
            inst.print(str, startx + 1, starty, col_bg);
            inst.print(str, startx - 1, starty, col_bg);
            inst.print(str, startx, starty + 1, col_bg);
            inst.print(str, startx, starty - 1, col_bg);
            inst.print(str, startx + 1, starty - 1, col_bg);
            inst.print(str, startx - 1, starty - 1, col_bg);
            inst.print(str, startx - 1, starty + 1, col_bg);
            inst.print(str, startx + 1, starty + 1, col_bg);
            inst.print(str, startx, starty, col);
        }

        static public bool btn_confirm()
        {
            return inst.btn(4) || inst.btn(6);
        }

        static public bool btnp_confirm()
        {
            return inst.btnp(4) || inst.btnp(6);
        }
    }
}
