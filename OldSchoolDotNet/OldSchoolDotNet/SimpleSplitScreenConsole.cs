using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace OldSchoolDotNet
{
    public class SimpleSplitScreenConsole
    {
        enum CtrlType
        {
            CTRL_C_EVENT = 0,
            CTRL_BREAK_EVENT = 1,
            CTRL_CLOSE_EVENT = 2,
            CTRL_LOGOFF_EVENT = 5,
            CTRL_SHUTDOWN_EVENT = 6
        }

        [DllImport("Kernel32")]
        private static extern bool SetConsoleCtrlHandler(EventHandler handler, bool add);

        private delegate bool EventHandler(CtrlType sig);

        private static bool Handler(CtrlType sig)
        {
            _running = false;

            _onShutDown();

            return true;
        }

        static EventHandler _handler;
        static Action _onShutDown = () => { };

        static List<string> _buffer = new List<string>();
        static int _bufferHeight = 0;
        static string _pid = "";
        static bool _running = true;
        static object _locker = new object();

        public static void InitConsole(IEnumerable<string> preamble, Action onShutDown)
        {
            _pid = Process.GetCurrentProcess().Id.ToString();

            _onShutDown = onShutDown;

            _handler += new EventHandler(Handler);
            SetConsoleCtrlHandler(_handler, true);

            _bufferHeight = Console.WindowHeight - 2;

            Console.Clear();

            foreach (var line in preamble)
            {
                Buffer(line);
            }

            // initialise the input area separately rather than in DrawScreen, or our
            // input will get wiped every time a message comes in and DrawScreen is
            // called.
            InitInputArea();
            DrawScreen();
        }

        public static void OnResponse(string message)
        {
            Buffer(message);
            DrawScreen();
        }

        // Console.ReadLine() blocks without chance of cancellation. This is
        // effectively a breakable ReadLine .. but it's surprisingly subtle. Check
        // out what happens when you type some text, then delete, without some
        // delete handling ...
        public static string FetchInput()
        {
            var b = new StringBuilder();

            while (_running)
            {
                if (Console.KeyAvailable)
                {
                    var ck = Console.ReadKey();
                    if (ck.Key == ConsoleKey.Backspace)
                    {
                        BlankRightInputArea();
                        b.Remove(b.Length - 1, 1);
                    }
                    else if (ck.Key == ConsoleKey.Enter)
                    {
                        InitInputArea();
                        break;
                    }
                    else
                    {
                        b.Append(ck.KeyChar);
                    }
                }
            }

            return b.ToString();

        }

        // this and the _buffer list effectively form a simple circular buffer.
        private static void Buffer(string line)
        {            
            _buffer.Add(line);

            if (_buffer.Count == _bufferHeight - 1)
            {
                _buffer.RemoveAt(0);
            }
        }

        private static void DrawScreen()
        {
            lock (_locker)
            {
                var left = Console.CursorLeft; var top = Console.CursorTop;

                StatusLine();

                var currentLine = _bufferHeight - _buffer.Count;

                for (int i = 0; i < _buffer.Count; i++)
                {
                    Console.SetCursorPosition(0, currentLine + i);
                    Console.Write("".PadRight(Console.BufferWidth, ' '));
                    if (_buffer[i] != null)
                    {
                        Console.SetCursorPosition(0, currentLine + i);
                        Console.Write(_buffer[i].Length > 0 ? _buffer[i].Substring(0, Math.Min(_buffer[i].Length, Console.BufferWidth)) : "");
                    }
                }

                // attempt to reset the cursor to where it was, to make input less
                // of a challenge ...
                Console.SetCursorPosition(left, top);
            }
        }

        private static void BlankRightInputArea()
        {
            lock(_locker)
            {
                var left = Console.CursorLeft; var top = Console.CursorTop;

                Console.SetCursorPosition(left, top);
                Console.Write(' ');

                Console.SetCursorPosition(left, top);
            }
        }

        private static void InitInputArea()
        {
            lock (_locker)
            {
                Console.SetCursorPosition(0, _bufferHeight + 2);
                Console.Write("".PadRight(Console.BufferWidth, ' '));
                Console.SetCursorPosition(0, _bufferHeight + 2);
                Console.Write("> ");
            }
        }

        private static void StatusLine()
        {
            Console.ForegroundColor = ConsoleColor.DarkGreen;

            var l = new StringBuilder();
            l.Append("-[").Append(_pid).Append("]");

            Console.SetCursorPosition(0, _bufferHeight + 1);
            Console.Write(l.ToString().PadRight(Console.BufferWidth, '-'));

            Console.ForegroundColor = ConsoleColor.Gray;
        }
    }
}
