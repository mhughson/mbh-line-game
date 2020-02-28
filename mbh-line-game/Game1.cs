using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using PicoX;
using System.Collections.Generic;
using System;
using TiledSharp;
using System.IO;
using System.Linq;
using Mono8;

namespace mbh_line_game
{
    public class Game1 : PicoXGame
    {
        public static Game1 inst;

        // Game State
        //

        public enum game_state
        {
            main_menu,
            gameplay,
            gameplay_dead,
            game_over,
            game_win,
        }

        game_state cur_game_state;
        uint time_in_state;

        //

        sprite p1;

        cam game_cam;

        public List<PicoXObj> objs;
        public List<PicoXObj> objs_remove_queue;
        public List<PicoXObj> objs_add_queue;

        public hit_pause_manager hit_pause;

        public bool debug_draw_enabled = false;

        // save game index info.
        public enum cartdata_index : uint
        {
            version = 0,
        }

        public int cur_save_version = 1;

        public Game1() : base()
        {
            // MUST BE DONE BEFORE ANY PICOXOBJ ARE CREATED
            inst = this;
        }

        public void set_game_state(game_state new_state)
        {
            cur_game_state = new_state;
            time_in_state = 0;
        }

        BufferedKey toggle_debug_draw = new BufferedKey(Keys.F1);

        public void clear_save()
        {
            // Zero's out all cartdata. Could do more complex logic if needed.
            for (uint i = 0; i < 64; i++)
            {
                dset(i, 0);
            }

            // version should be set, even on a clear save.
            dset((uint)cartdata_index.version, cur_save_version);
        }

        public override void _init()
        {
            // Create save file.
            cartdata("mbh-line-game");

            // Zero's out all cartdata. Could do more complex logic if needed.
            Action clear_save_del = delegate ()
            {
                clear_save();
            };

            // Add ability for user to clear save data.
            menuitem(1, "clear save", clear_save_del);

            int ver = dget((uint)cartdata_index.version);

            // If this is an old version, clear it.
            // Ideally this would not just clear the save but rather "upgrade it".
            if (ver < cur_save_version)
            {
                clear_save();
            }
            
            objs = new List<PicoXObj>();
            objs_remove_queue = new List<PicoXObj>();
            objs_add_queue = new List<PicoXObj>();

            p1 = new player();
            game_cam = new cam(p1);

            objs_add_queue.Add(p1);

            hit_pause = new hit_pause_manager();

            set_game_state(game_state.main_menu);
        }

        public override void _update60()
        {
            time_in_state++;

#if DEBUG
            if (toggle_debug_draw.Update())
            {
                debug_draw_enabled = !debug_draw_enabled;
            }
#endif


            // flip before iterating backwards. This is slight wrong, since hidden items will get flipped
            // multiple times.
            // NOTE: This comes BEFORE objects get updated to avoid case where an object is
            //       added to objs list, but isn't updated, and then gets drawn with garbage
            //       data.
            objs_add_queue.Reverse();
            for (int i = objs_add_queue.Count - 1; i >= 0; i--)
            {
                sprite s = objs_add_queue[i] as sprite;
                objs.Add(objs_add_queue[i]);
                objs_add_queue.RemoveAt(i);
            }

            // TODO: Should we ignore objects in the remove queue?
            for (int i = 0; i < objs.Count; i++)
            {
                objs[i]._preupdate();
            }
            for (int i = 0; i < objs.Count; i++)
            {
                objs[i]._update60();
            }
            for (int i = 0; i < objs.Count; i++)
            {
                objs[i]._postupdate();
            }

            // Remove all the objects which requested to be removed.
            objs = objs.Except(objs_remove_queue).ToList();
            objs_remove_queue.Clear();

            if (game_cam != null)
            {
                game_cam._update60();
            }
            if (hit_pause != null)
            {
                hit_pause._update60();
            }
        }

        public override void _draw()
        {
            pal();
            //cls(7);

            float cam_x = 0; // game_cam.cam_pos().X;
            float cam_y = 0; // game_cam.cam_pos().Y;

            camera(cam_x, cam_y);

            foreach (PicoXObj o in objs)
            {
                if (o is sprite)
                {
                    if (game_cam == null || game_cam.is_obj_in_play_area(o as sprite))
                    {
                        (o as sprite).push_pal();
                        o._draw();
                        (o as sprite).pop_pal();
                    }
                }
            }

            pset(p1.x, p1.y, 8);

            if (game_cam != null)
            {
                game_cam._draw();
            }

            // Map grid.
            if (debug_draw_enabled)
            {
                int start_x = flr(cam_x / 16.0f) * 16;
                int start_y = flr(cam_y / 16.0f) * 16;
                for (int x = start_x; x <= start_x + Res.X; x += 16)
                {
                    line(x, start_y, x, start_y + Res.Y + 16, 2);
                }
                for (int y = start_y; y <= start_y + Res.Y; y += 16)
                {
                    line(start_x, y, start_x + Res.X + 16, y, 2);
                }
            }

            // HUD

            camera(0, 0);

            if (debug_draw_enabled)
            {
                string btnstr = "";
                for (int i = 0; i < 6; i++)
                {
                    btnstr += btn(i) ? "1" : "0";
                    btnstr += " ";
                }

                print(btnstr, 0, Res.Y - 5, 0);

                print(objs.Count.ToString(), btnstr.Length * 4, Res.Y - 5, 1);
            }
        }

        public override string GetMapString()
        {
            return "";
        }

        public override Dictionary<int, string> GetMusicPaths()
        {
            return new Dictionary<int, string>();
        }

        public override List<string> GetSheetPath()
        {
            return new List<string>() { @"raw\sprites_00" };
        }

        public override Dictionary<int, string> GetSoundEffectPaths()
        {
            return new Dictionary<int, string>();
        }

        public override Dictionary<string, object> GetScriptFunctions()
        {
            Dictionary<string, object> Funcs = new Dictionary<string, object>();
            return Funcs;
        }

        public override string GetPalTextureString()
        {
            return "";
        }

        // note: we want to make sure the width and height at multiples of 16 to ensure tiles go right to the edge.
        //public Vector2 Res = new Vector2(448, 240); // NES WS
        public Vector2 Res = new Vector2(256, 240); // NES
        //public Vector2 Res = new Vector2(160, 144); // GB
        //public Vector2 Res = new Vector2(256, 144); // GB WS

        public override Tuple<int, int> GetResolution() { return new Tuple<int, int>((int)Res.X, (int)Res.Y); }

        public override int GetGifScale() { return 2; }

        public override bool GetGifCaptureEnabled()
        {
            return true;
        }

        public override bool GetPauseMenuEnabled()
        {
            return false;
        }
    }
}

