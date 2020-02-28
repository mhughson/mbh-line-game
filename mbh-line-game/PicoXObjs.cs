using Microsoft.Xna.Framework;
using PicoX;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static mbh_line_game.Game1;

namespace mbh_line_game
{
    public class PicoXObj : PicoXGame
    {
        #region UNUSED_API
        public override string GetMapString()
        {
            throw new NotImplementedException();
        }

        public override Dictionary<int, string> GetMusicPaths()
        {
            throw new NotImplementedException();
        }

        public override string GetPalTextureString()
        {
            throw new NotImplementedException();
        }

        public override Dictionary<string, object> GetScriptFunctions()
        {
            throw new NotImplementedException();
        }

        public override List<string> GetSheetPath()
        {
            throw new NotImplementedException();
        }

        public override Dictionary<int, string> GetSoundEffectPaths()
        {
            throw new NotImplementedException();
        }
        #endregion

        public PicoXObj()
        {
            if (inst == null)
            {
                throw new InvalidOperationException("Attempting to create PicoXObj before inst is set.");
            }
            P8API = inst;
        }

        public virtual void _preupdate() { }
        public virtual void _postupdate() { }
    }

    public class sprite : PicoXObj
    {
        public class flying_def
        {
            public bool horz = true;
            public int duration = 0;
            public int dist = 0;
        }
        public float x;
        public float y;
        public float x_initial;
        public float y_initial;
        public float dx;
        public float dy;
        public int w;
        public int h;
        public int cw;
        public int ch;
        public int cx_offset { get; protected set; } = 0;
        public int cy_offset { get; protected set; } = 0;
        public float cx { get { return x + cx_offset; } }
        public float cy { get { return y + cy_offset; } }
        public int jump_hold_time = 0;//how long jump is held
        public byte grounded = 0;//on ground
        public int airtime = 0;//time since groundeds
        public float scaley = 0;

        public bool is_platform = false;
        public bool stay_on = false;
        public bool launched = false;

        public flying_def flying = null;
        public virtual int get_hp_max() { return 1; }
        public float hp = 1;
        public float attack_power = 1;

        protected int invul_time = 0;
        protected int invul_time_on_hit = 120;

        public int bank = 0;

        public class anim
        {
            public int ticks;
            public int[][] frames;
            public bool? loop;
            public int? w;
            public int? h;
        }

        static public int[] create_anim_frame(int start_sprite, int w, int h, int zeroed_rows_at_top = 0)
        {
            int[] frame = new int[w * h];
            int count = 0;
            for (int j = 0; j < h; j++)
            {
                for (int i = 0; i < w; i++)
                {
                    int sprite_id = (start_sprite + i) + (16 * j);
                    if (j < zeroed_rows_at_top)
                    {
                        sprite_id = 0;
                    }
                    frame[count] = sprite_id;
                    count++;
                }
            }

            return frame;
        }

        //animation definitions.
        //use with set_anim()
        public Dictionary<string, anim> anims;

        public string curanim = "";//currently playing animation
        public int curframe = 0;//curent frame of animation.
        public int animtick = 0;//ticks until next frame should show.
        public bool flipx = false;//show sprite be flipped.
        public bool flipy = false;

        public delegate void on_anim_done_delegate(string anim_name);
        public on_anim_done_delegate event_on_anim_done;

        //request new animation to play.
        public void set_anim(string anim)
        {
            var self = this;
            if (anim == self.curanim) return;//early out.
            var a = self.anims[anim];

            self.animtick = a.ticks;//ticks count down.
            self.curanim = anim;
            self.curframe = 0;
        }

        public void tick_anim()
        {
            if (anims == null || !anims.ContainsKey(curanim))
            {
                return;
            }

            animtick -= 1;
            if (animtick <= 0)
            {
                curframe += 1;

                var a = anims[curanim];
                animtick = a.ticks; //reset timer
                if (curframe >= a.frames.Length)
                {
                    if (a.loop == false)
                    {
                        // back up the frame counter so that we sit on the last frame.
                        // do it before calling anim_done, because that might actually trigger
                        // a new animation and we don't want to mess with its frame.
                        curframe--;
                        event_on_anim_done?.Invoke(curanim); // TODO_PORT
                    }
                    else
                    {
                        // TODO: Was it intentional that this is only called when looping?
                        event_on_anim_done?.Invoke(curanim); // TODO_PORT
                        curframe = 0; //loop
                    }
                }
            }
        }

        public override void _update60()
        {
            invul_time = (int)max(0, invul_time - 1);
            base._update60();

            tick_anim();
        }

        public virtual void push_pal()
        {
            //inst.apply_pal(inst.get_cur_pal(true));
        }

        public virtual void pop_pal()
        {
            pal();
        }

        public override void _draw()
        {
            var self = this;
            base._draw();

            inst.bset(bank);

            if (anims != null && !String.IsNullOrEmpty(curanim))
            {

                var a = anims[curanim];
                int[] frame = a.frames[curframe];

                // TODO: Mono8 Port
                //if (pal) push_pal(pal)

                // Mono8 Port: Starting with table only style.
                //if type(frame) == "table" then
                if (invul_time == 0 || invul_time % 2 == 0)
                {
                    var final_w = a.w ?? w;
                    var final_h = a.h ?? h;
                    var final_w_half = flr(final_w * 0.5f);
                    var final_h_half = flr(final_h * 0.5f);

                    var start_x = x - (final_w_half);
                    var start_y = y - (final_h_half);

                    var count = 0;

                    var num_vert = flr(final_h / 8);
                    var num_horz = flr(final_w / 8);

                    var inc_x = 8;
                    var inc_y = 8;

                    if (flipx)
                    {
                        start_x = start_x + ((num_horz - 1) * 8);
                        inc_x = -8;
                    }


                    if (flipy)
                    {
                        start_y = start_y + ((num_vert - 1) * 8);
                        inc_y = -8;
                    }

                    var y2 = start_y;

                    for (int v_count = 0; v_count < num_vert; v_count++)
                    {
                        var x2 = start_x;

                        for (int h_count = 0; h_count < num_horz; h_count++)
                        {
                            //draw in frame order, but from
                            // right to left.
                            var f = frame[count];

                            // Don't draw sprite 0. This allows us to use that as a special 
                            // sprite in our animation data.
                            if (f != 0)
                            {

                                var flipx2 = flipx;

                                var flipy2 = flipy;

                                // Mono8 Port: frame is an int can can't be null.
                                //if (f != null)
                                {
                                    // TODO: This doesn't properly support flipping collections of tiles (eg. turn a 3 tile high 
                                    // sprite upside down. it will flip each tile independently).
                                    if (f < 0)
                                    {
                                        f = (int)abs(f);

                                        flipx2 = !flipx2;
                                    }

                                    // Hack to allow flipping Y. Add 512 to your sprite id.
                                    if (f >= 9999)
                                    {
                                        f -= 9999;

                                        flipy2 = !flipy2;
                                    }

                                    sspr((f * 8) % 128, flr((f / 16)) * 8, 8, 8,
                                        x2, y2 - (scaley * v_count) + (((8) - (8 - scaley)) * num_vert), 8, 8 - scaley,
                                        flipx2, flipy2);

                                }
                            }
                            count += 1;

                            x2 += inc_x;

                        }
                        y2 += inc_y;

                    }
                }
            }

            if (inst.debug_draw_enabled)
            {
                //if (inst.time_in_state % 2 == 0)
                {
                    rect(x - w / 2, y - h / 2, x + w / 2, y + h / 2, 14);
                    rect(cx - cw / 2, cy - ch / 2, cx + cw / 2, cy + ch / 2, 15);
                }
                pset(x, y, 8);
                pset(cx, cy, 9);

                // bottom
                //var offset_x = self.cw / 3.0f;
                //var offset_y = self.ch / 2.0f;
                //for (float i = -(offset_x); i <= (offset_x); i += 2)
                //{
                //    pset(x + i, y + offset_y, 9);
                //}

                // sides
            }
        }

        public virtual void on_collide_side(sprite target) { }
    }

    public class actor : sprite
    {
        public actor() : base()
        {

        }
    }

    public class player : actor
    {
        public player() : base()
        {

        }

        public override void _update60()
        {
            base._update60();

            float speed = 1;
            if (btn(0))
            {
                x = (max(0, x - speed));
            }
            if (btn(1))
            {
                x = (min(inst.GetResolution().Item1 - 1, x + speed));
            }
            if (btn(2))
            {
                y = (max(0, y - speed));
            }
            if (btn(3))
            {
                y = (min(inst.GetResolution().Item2 - 1, y + speed));
            }
        }
    }

    public class simple_fx : sprite
    {
        public simple_fx()
        {
            anims = new Dictionary<string, anim>()
                {
                    {
                        "explode",
                        new anim()
                        {
                            loop = false,
                            ticks=4,//how long is each frame shown.
                            //frames= new int[][] { new int[] { 0, 1, 2, 3, 16, 17, 18, 19, 32, 33, 34, 35 } },//what frames are shown.
                            frames = new int[][]
                            {
                                create_anim_frame(112, 4, 3),
                                create_anim_frame(116, 4, 3),
                                create_anim_frame(120, 4, 3),
                            }
                        }
                    },
                };

            set_anim("explode");

            x = 64;
            y = 64;
            w = 32;
            h = 24;

            event_on_anim_done += delegate (string anim_name)
            {
                inst.objs_remove_queue.Add(this);
            };
        }

        public override void _update60()
        {
            if (inst.hit_pause.is_paused())
            {
                return;
            }

            base._update60();
        }
    }

    //make the camera.
    public class cam : PicoXObj
    {

        sprite tar;//target to follow.
        Vector2 pos;

        //how far from center of screen target must
        //be before camera starts following.
        //allows for movement in center without camera
        //constantly moving.
        public float pull_threshold = 16;

        public Vector2 pull_threshold_offset = Vector2.Zero;

        //min and max positions of camera.
        //the edges of the level.
        public Vector2 pos_min = new Vector2(inst.Res.X * 0.5f, inst.Res.Y * 0.5f);
        public Vector2 pos_max = new Vector2(368 - inst.Res.X * 0.5f, 1024 - inst.Res.Y * 0.5f);

        public Vector2 play_area_min = Vector2.Zero;
        public Vector2 play_area_max = Vector2.Zero;

        int shake_remaining = 0;
        float shake_force = 0;

        public cam(sprite target)
        {
            tar = target;
            jump_to_target();
        }
        public void jump_to_target()
        {
            pos = new Vector2(tar.x, tar.y);
        }
        public override void _update60()
        {
            var self = this;

            base._update60();

            self.shake_remaining = (int)max(0, self.shake_remaining - 1);

            float max_cam_speed = 88888.0f;

            if (self.tar != null)
            {

                //follow target outside of
                //pull range.
                if (pull_max_x() < self.tar.x)
                {

                    self.pos.X += min(self.tar.x - pull_max_x(), max_cam_speed);

                }
                if (pull_min_x() > self.tar.x)
                {
                    self.pos.X += min((self.tar.x - pull_min_x()), max_cam_speed);
                }


                if (pull_max_y() < self.tar.y)
                {
                    self.pos.Y += min(self.tar.y - pull_max_y(), max_cam_speed);

                }
                if (pull_min_y() > self.tar.y)
                {
                    self.pos.Y += min((self.tar.y - pull_min_y()), max_cam_speed);

                }
            }

            //lock to edge
            if (self.pos.X < self.pos_min.X) self.pos.X = self.pos_min.X;
            if (self.pos.X > self.pos_max.X) self.pos.X = self.pos_max.X;
            if (self.pos.Y < self.pos_min.Y) self.pos.Y = self.pos_min.Y;
            if (self.pos.Y > self.pos_max.Y) self.pos.Y = self.pos_max.Y;

        }

        //public void activate_objs()
        //{
        //    for (int i = Game.game_world.cur_area.objs_queue.Count - 1; i >= 0; i--)
        //    {
        //        obj v = Game.game_world.cur_area.objs_queue[i];

        //        Rectangle area = spawn_rect();
        //        if (v.x <= (area.Right + v.w_half()) && v.x >= (area.Left - v.w_half()) && v.y <= (area.Bottom + v.h_half()) && v.y >= (area.Top - v.h_half()))
        //        //if (v.x < x)
        //        {
        //            // move to active list.
        //            v.activate();
        //        }
        //    }
        //}

        public override void _draw()
        {
            base._draw();
#if DEBUG
            if (inst.debug_draw_enabled)
            {
                rect(pos_min.X, pos_min.Y, pos_max.X - 1, pos_max.Y - 1, 8);
                rect(play_area_min.X, play_area_min.Y, play_area_max.X - 1, play_area_max.Y - 1, 9);
            }
#endif // DEBUG
        }

        public Vector2 cam_pos()
        {

            var self = this;
            //calculate camera shake.
            var shk = new Vector2(0, 0);
            if (self.shake_remaining > 0)
            {
                shk.X = rnd(self.shake_force) - (self.shake_force / 2);
                shk.Y = rnd(self.shake_force) - (self.shake_force / 2);

            }
            return new Vector2(self.pos.X - (inst.Res.X * 0.5f) + shk.X, self.pos.Y - (inst.Res.Y * 0.5f) + shk.Y);

        }

        public float pull_max_x()
        {
            return (pos.X + pull_threshold_offset.X) + pull_threshold;
        }

        public float pull_min_x()
        {
            return (pos.X + pull_threshold_offset.X) - pull_threshold;
        }

        public float pull_max_y()
        {
            return (pos.Y + pull_threshold_offset.Y) + pull_threshold;
        }

        public float pull_min_y()
        {
            return (pos.Y + pull_threshold_offset.Y) - pull_threshold;

        }

        public void shake(int ticks, float force)
        {
            shake_remaining = ticks;

            shake_force = force;
        }

        public bool is_obj_off_screen(sprite s)
        {
            return !math_utils.intersects_obj_box(s, pos.X, pos.Y, inst.Res.X * 0.5f, inst.Res.Y * 0.5f);
        }

        public bool is_pos_off_screen(float x, float y)
        {
            return !math_utils.intersects_point_box(x, y, pos.X, pos.Y, inst.Res.X * 0.5f, inst.Res.Y * 0.5f);
        }

        public bool is_pos_in_play_area(float x, float y)
        {
            Vector2 play_area_size = (play_area_max - play_area_min) * 0.5f;
            return math_utils.intersects_point_box(x, y, play_area_min.X + play_area_size.X, play_area_min.Y + play_area_size.Y, play_area_size.X, play_area_size.Y);
        }

        public bool is_obj_in_play_area(sprite s)
        {
            Vector2 play_area_size = (play_area_max - play_area_min) * 0.5f;
            return math_utils.intersects_obj_box(s, play_area_min.X + play_area_size.X, play_area_min.Y + play_area_size.Y, play_area_size.X, play_area_size.Y);
        }

        public bool is_obj_in_play_area(sprite s, Vector2 offset)
        {
            Vector2 play_area_size = (play_area_max - play_area_min) * 0.5f;
            return math_utils.intersects_obj_box(s, play_area_min.X + play_area_size.X + offset.X, play_area_min.Y + play_area_size.Y + offset.Y, play_area_size.X, play_area_size.Y);
        }
    }
}
