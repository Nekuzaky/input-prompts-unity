using System.Collections.Generic;

namespace Nekuzaky.InputPrompts.Editor
{
    /// <summary>
    /// Maps Input System control keys to file names of the Kenney "Input Prompts" pack.
    /// Only used by the importer: once the prompt sets are generated, nothing here runs at runtime.
    /// </summary>
    public static class KenneyNameTable
    {
        #region Main API

        /// <summary>Sub-folder of the pack holding the icons of a style.</summary>
        public static string FolderFor(InputDeviceStyle style) => style switch
        {
            InputDeviceStyle.KeyboardMouse => "Keyboard & Mouse",
            InputDeviceStyle.Xbox => "Xbox Series",
            InputDeviceStyle.PlayStation => "PlayStation Series",
            InputDeviceStyle.Switch => "Nintendo Switch",
            InputDeviceStyle.SteamDeck => "Steam Deck",
            InputDeviceStyle.SteamController => "Steam Controller",
            InputDeviceStyle.Touch => "Touch",
            _ => "Generic",
        };

        /// <summary>Device layouts a style should be picked for.</summary>
        public static string[] LayoutsFor(InputDeviceStyle style) => style switch
        {
            InputDeviceStyle.KeyboardMouse => new[] { "Keyboard", "Mouse" },
            InputDeviceStyle.Xbox => new[] { "XInputController" },
            InputDeviceStyle.PlayStation => new[] { "DualShockGamepad" },
            InputDeviceStyle.Switch => new[] { "SwitchProControllerHID" },
            InputDeviceStyle.SteamDeck => new[] { "SteamDeckGamepad" },
            InputDeviceStyle.SteamController => new[] { "SteamController" },
            InputDeviceStyle.Touch => new[] { "Touchscreen" },
            _ => new[] { "Gamepad" },
        };

        /// <summary>Icon used as a background when a control has no icon of its own.</summary>
        public static string BlankFor(InputDeviceStyle style) => style switch
        {
            InputDeviceStyle.KeyboardMouse => "keyboard",
            InputDeviceStyle.Xbox => "xbox_button_color_a",
            InputDeviceStyle.PlayStation => "playstation_button_analog",
            InputDeviceStyle.Switch => "switch_button_a",
            InputDeviceStyle.SteamDeck => "steamdeck_button_a",
            _ => "generic_button",
        };

        /// <summary>control key -> Kenney file name (without extension or "_outline" suffix).</summary>
        public static Dictionary<string, string> For(InputDeviceStyle style, bool coloredFaceButtons)
        {
            return style switch
            {
                InputDeviceStyle.KeyboardMouse => KeyboardAndMouse(),
                InputDeviceStyle.Xbox => Xbox(coloredFaceButtons),
                InputDeviceStyle.PlayStation => PlayStation(coloredFaceButtons),
                InputDeviceStyle.Switch => Switch(),
                InputDeviceStyle.SteamDeck => SteamDeck(),
                InputDeviceStyle.Generic => Generic(),
                _ => new Dictionary<string, string>(),
            };
        }

        #endregion


        #region Tools and Utilities

        private static Dictionary<string, string> KeyboardAndMouse()
        {
            var map = new Dictionary<string, string>();

            for (var c = 'a'; c <= 'z'; c++)
                map[c.ToString()] = $"keyboard_{c}";
            for (var d = 0; d <= 9; d++)
            {
                map[d.ToString()] = $"keyboard_{d}";
                map[$"numpad{d}"] = $"keyboard_{d}";
            }
            for (var f = 1; f <= 12; f++)
                map[$"f{f}"] = $"keyboard_f{f}";

            Add(map, new (string, string)[]
            {
                ("space", "keyboard_space"),
                ("enter", "keyboard_enter"),
                ("escape", "keyboard_escape"),
                ("tab", "keyboard_tab"),
                ("backspace", "keyboard_backspace"),
                ("capslock", "keyboard_capslock"),
                ("numlock", "keyboard_numlock"),
                ("scrolllock", "keyboard_scroll_lock"),
                ("printscreen", "keyboard_printscreen"),
                ("pause", "keyboard_pause"),
                ("insert", "keyboard_insert"),
                ("delete", "keyboard_delete"),
                ("home", "keyboard_home"),
                ("end", "keyboard_end"),
                ("pageup", "keyboard_page_up"),
                ("pagedown", "keyboard_page_down"),
                ("leftshift", "keyboard_shift"),
                ("rightshift", "keyboard_shift"),
                ("shift", "keyboard_shift"),
                ("leftctrl", "keyboard_ctrl"),
                ("rightctrl", "keyboard_ctrl"),
                ("ctrl", "keyboard_ctrl"),
                ("leftalt", "keyboard_alt"),
                ("rightalt", "keyboard_alt"),
                ("alt", "keyboard_alt"),
                ("leftmeta", "keyboard_win"),
                ("rightmeta", "keyboard_win"),
                ("leftwindows", "keyboard_win"),
                ("rightwindows", "keyboard_win"),
                ("leftapple", "keyboard_command"),
                ("rightapple", "keyboard_command"),
                ("leftcommand", "keyboard_command"),
                ("rightcommand", "keyboard_command"),
                ("contextmenu", "keyboard_function"),
                ("uparrow", "keyboard_arrow_up"),
                ("downarrow", "keyboard_arrow_down"),
                ("leftarrow", "keyboard_arrow_left"),
                ("rightarrow", "keyboard_arrow_right"),
                ("backquote", "keyboard_tilde"),
                ("quote", "keyboard_quote"),
                ("semicolon", "keyboard_semicolon"),
                ("comma", "keyboard_comma"),
                ("period", "keyboard_period"),
                ("slash", "keyboard_slash_forward"),
                ("backslash", "keyboard_slash_back"),
                ("leftbracket", "keyboard_bracket_open"),
                ("rightbracket", "keyboard_bracket_close"),
                ("minus", "keyboard_minus"),
                ("equals", "keyboard_equals"),
                ("numpadenter", "keyboard_numpad_enter"),
                ("numpadplus", "keyboard_numpad_plus"),
                ("numpadminus", "keyboard_minus"),
                ("numpadmultiply", "keyboard_asterisk"),
                ("numpaddivide", "keyboard_slash_forward"),
                ("numpadperiod", "keyboard_period"),
                ("numpadequals", "keyboard_equals"),
                ("anykey", "keyboard_any"),

                // Mouse
                ("leftbutton", "mouse_left"),
                ("rightbutton", "mouse_right"),
                ("middlebutton", "mouse_scroll"),
                ("forwardbutton", "mouse_side_forward"),
                ("backbutton", "mouse_side_back"),
                ("press", "mouse_left"),
                ("scroll", "mouse_scroll"),
                ("scroll/up", "mouse_scroll_up"),
                ("scroll/down", "mouse_scroll_down"),
                ("scroll/y", "mouse_scroll_vertical"),
                ("scroll/x", "mouse_horizontal"),
                ("position", "mouse_move"),
                ("delta", "mouse_move"),
                ("delta/up", "mouse_vertical"),
                ("delta/down", "mouse_vertical"),
                ("delta/left", "mouse_horizontal"),
                ("delta/right", "mouse_horizontal"),
                ("delta/x", "mouse_horizontal"),
                ("delta/y", "mouse_vertical"),
            });

            return map;
        }

        // ------------------------------------------------------------------ gamepads

        private static Dictionary<string, string> Xbox(bool colored)
        {
            var south = colored ? "xbox_button_color_a" : "xbox_button_a";
            var east = colored ? "xbox_button_color_b" : "xbox_button_b";
            var west = colored ? "xbox_button_color_x" : "xbox_button_x";
            var north = colored ? "xbox_button_color_y" : "xbox_button_y";

            var map = Gamepad("xbox");
            Add(map, new (string, string)[]
            {
                ("buttonsouth", south), ("a", south), ("cross", south),
                ("buttoneast", east), ("b", east), ("circle", east),
                ("buttonwest", west), ("x", west), ("square", west),
                ("buttonnorth", north), ("y", north), ("triangle", north),
                ("leftshoulder", "xbox_lb"), ("l1", "xbox_lb"),
                ("rightshoulder", "xbox_rb"), ("r1", "xbox_rb"),
                ("lefttrigger", "xbox_lt"), ("l2", "xbox_lt"),
                ("righttrigger", "xbox_rt"), ("r2", "xbox_rt"),
                ("start", "xbox_button_menu"), ("menu", "xbox_button_menu"),
                ("select", "xbox_button_view"), ("view", "xbox_button_view"), ("share", "xbox_button_share"),
                ("systembutton", "xbox_guide"), ("guide", "xbox_guide"),
            });
            return map;
        }

        private static Dictionary<string, string> PlayStation(bool colored)
        {
            var south = colored ? "playstation_button_color_cross" : "playstation_button_cross";
            var east = colored ? "playstation_button_color_circle" : "playstation_button_circle";
            var west = colored ? "playstation_button_color_square" : "playstation_button_square";
            var north = colored ? "playstation_button_color_triangle" : "playstation_button_triangle";

            var map = Gamepad("playstation");
            Add(map, new (string, string)[]
            {
                ("buttonsouth", south), ("cross", south), ("a", south),
                ("buttoneast", east), ("circle", east), ("b", east),
                ("buttonwest", west), ("square", west), ("x", west),
                ("buttonnorth", north), ("triangle", north), ("y", north),
                ("leftshoulder", "playstation_trigger_l1"), ("l1", "playstation_trigger_l1"),
                ("rightshoulder", "playstation_trigger_r1"), ("r1", "playstation_trigger_r1"),
                ("lefttrigger", "playstation_trigger_l2"), ("l2", "playstation_trigger_l2"),
                ("righttrigger", "playstation_trigger_r2"), ("r2", "playstation_trigger_r2"),
                ("leftstickpress", "playstation_button_l3"), ("l3", "playstation_button_l3"),
                ("rightstickpress", "playstation_button_r3"), ("r3", "playstation_button_r3"),
                ("start", "playstation4_button_options"), ("options", "playstation4_button_options"),
                ("select", "playstation4_button_share"), ("share", "playstation4_button_share"),
                ("create", "playstation5_button_create"),
                ("touchpadbutton", "playstation4_touchpad_press"), ("touchpad", "playstation4_touchpad"),
                ("systembutton", "playstation_button_analog"),
            });
            return map;
        }

        private static Dictionary<string, string> Switch()
        {
            // Nintendo swaps the face buttons: what Unity calls buttonSouth is physically B.
            var map = Gamepad("switch");
            Add(map, new (string, string)[]
            {
                ("buttonsouth", "switch_button_b"), ("b", "switch_button_b"), ("cross", "switch_button_b"),
                ("buttoneast", "switch_button_a"), ("a", "switch_button_a"), ("circle", "switch_button_a"),
                ("buttonwest", "switch_button_y"), ("y", "switch_button_y"), ("square", "switch_button_y"),
                ("buttonnorth", "switch_button_x"), ("x", "switch_button_x"), ("triangle", "switch_button_x"),
                ("leftshoulder", "switch_button_l"), ("l", "switch_button_l"), ("l1", "switch_button_l"),
                ("rightshoulder", "switch_button_r"), ("r", "switch_button_r"), ("r1", "switch_button_r"),
                ("lefttrigger", "switch_button_zl"), ("zl", "switch_button_zl"), ("l2", "switch_button_zl"),
                ("righttrigger", "switch_button_zr"), ("zr", "switch_button_zr"), ("r2", "switch_button_zr"),
                ("start", "switch_button_plus"), ("plus", "switch_button_plus"),
                ("select", "switch_button_minus"), ("minus", "switch_button_minus"),
                ("systembutton", "switch_button_home"), ("home", "switch_button_home"),
                ("capture", "switch_button_sync"),
            });
            return map;
        }

        private static Dictionary<string, string> SteamDeck()
        {
            var map = Gamepad("steamdeck");
            Add(map, new (string, string)[]
            {
                ("buttonsouth", "steamdeck_button_a"), ("a", "steamdeck_button_a"),
                ("buttoneast", "steamdeck_button_b"), ("b", "steamdeck_button_b"),
                ("buttonwest", "steamdeck_button_x"), ("x", "steamdeck_button_x"),
                ("buttonnorth", "steamdeck_button_y"), ("y", "steamdeck_button_y"),
                ("leftshoulder", "steamdeck_button_l1"), ("rightshoulder", "steamdeck_button_r1"),
                ("lefttrigger", "steamdeck_button_l2"), ("righttrigger", "steamdeck_button_r2"),
                ("start", "steamdeck_button_options"), ("select", "steamdeck_button_view"),
                ("systembutton", "steamdeck_button_guide"),
            });
            return map;
        }

        private static Dictionary<string, string> Generic()
        {
            var map = new Dictionary<string, string>();
            Add(map, new (string, string)[]
            {
                ("buttonsouth", "generic_button_circle"),
                ("buttoneast", "generic_button_circle"),
                ("buttonwest", "generic_button_circle"),
                ("buttonnorth", "generic_button_circle"),
                ("leftshoulder", "generic_button_trigger_a"),
                ("rightshoulder", "generic_button_trigger_a"),
                ("lefttrigger", "generic_button_trigger_b"),
                ("righttrigger", "generic_button_trigger_b"),
                ("start", "generic_button_square"),
                ("select", "generic_button_square"),
                ("leftstick", "generic_stick"),
                ("rightstick", "generic_stick"),
                ("leftstick/up", "generic_stick_up"),
                ("leftstick/down", "generic_stick_down"),
                ("leftstick/left", "generic_stick_left"),
                ("leftstick/right", "generic_stick_right"),
                ("leftstick/x", "generic_stick_horizontal"),
                ("leftstick/y", "generic_stick_vertical"),
                ("rightstick/up", "generic_stick_up"),
                ("rightstick/down", "generic_stick_down"),
                ("rightstick/left", "generic_stick_left"),
                ("rightstick/right", "generic_stick_right"),
                ("rightstick/x", "generic_stick_horizontal"),
                ("rightstick/y", "generic_stick_vertical"),
                ("leftstickpress", "generic_stick_press"),
                ("rightstickpress", "generic_stick_press"),
            });
            return map;
        }

        /// <summary>Sticks and d-pad, which follow the same naming pattern on every Kenney controller sheet.</summary>
        private static Dictionary<string, string> Gamepad(string prefix)
        {
            var map = new Dictionary<string, string>();

            foreach (var (control, suffix) in new[] { ("leftstick", "l"), ("rightstick", "r") })
            {
                var stick = $"{prefix}_stick_{suffix}";
                map[control] = stick;
                map[$"{control}/up"] = $"{stick}_up";
                map[$"{control}/down"] = $"{stick}_down";
                map[$"{control}/left"] = $"{stick}_left";
                map[$"{control}/right"] = $"{stick}_right";
                map[$"{control}/x"] = $"{stick}_horizontal";
                map[$"{control}/y"] = $"{stick}_vertical";
                map[$"{control}press"] = $"{stick}_press";
            }

            map["dpad"] = $"{prefix}_dpad";
            map["dpad/up"] = $"{prefix}_dpad_up";
            map["dpad/down"] = $"{prefix}_dpad_down";
            map["dpad/left"] = $"{prefix}_dpad_left";
            map["dpad/right"] = $"{prefix}_dpad_right";
            map["dpad/x"] = $"{prefix}_dpad_horizontal";
            map["dpad/y"] = $"{prefix}_dpad_vertical";

            return map;
        }

        private static void Add(Dictionary<string, string> map, (string key, string file)[] entries)
        {
            foreach (var (key, file) in entries)
                map[key] = file;
        }

        #endregion
    }
}
