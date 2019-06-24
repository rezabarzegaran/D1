using Svg;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace Planner.Objects
{
    public class SVGDump
    {
        private int _simulationlength;
        private readonly int _startx;
        private readonly int _starty;
        private readonly int _cycleWidth;
        private readonly SvgDocument _document;
        private readonly List<SvgRectangle> _cpus;
        private readonly List<SvgRectangle> _cores;
        private readonly List<SvgText> _texts;
        private readonly List<SvgLine> _lines;
        private readonly Color[] _taskcolors;
        private readonly List<int> _coreoffset;

        public SVGDump()
        {
            _document = new SvgDocument();
            _cores = new List<SvgRectangle>();
            _cpus = new List<SvgRectangle>();
            _texts = new List<SvgText>();
            _lines = new List<SvgLine>();
            _startx = 50;
            _starty = 0;
            _cycleWidth = 10;
            _taskcolors = new Color[]{Color.Blue,Color.BlueViolet,Color.Red,
                Color.Coral,Color.CornflowerBlue,Color.DarkBlue,Color.DarkOliveGreen,
                Color.Chocolate,Color.Chartreuse,Color.DarkGoldenrod,Color.DarkGreen,Color.CadetBlue,
                Color.DarkRed,Color.DarkKhaki,Color.Yellow};
            _coreoffset = new List<int>() { 0 };
        }

        public void SetScope(int simulationlength)
        {
            _simulationlength = simulationlength;
        }
        public void AddCPU(int id, int cores)
        {
            SvgRectangle cpu = new SvgRectangle
            {
                Fill = new SvgColourServer(_cpus.Count % 2 == 0 ? Color.FromArgb(255, 242, 242, 242) : Color.FromArgb(255, 232, 232, 232)),
                FillOpacity = 1,
                X = 0,
                Y = _cycleWidth * _cores.Count,
                Height = _cycleWidth * cores,
                Width = _startx
            };
            int offset = _coreoffset.Last();
            _coreoffset.Add(offset + (_cycleWidth * cores));
            SvgText cpuText = new SvgText
            {
                Text = $"CPU{id}",
                X = new SvgUnitCollection() { 5 },
                Y = new SvgUnitCollection() { (_cycleWidth * _cores.Count) + ((cores * _cycleWidth) / 2) + 8 },
                FontSize = 16,
                FontStyle = SvgFontStyle.Normal
            };
            for (int core = 0; core < cores; core++)
            {
                Color col = _cores.Count % 2 == 0 ? Color.FromArgb(255, 255, 255, 255) : Color.FromArgb(255, 242, 242, 242);
                SvgRectangle rectangles = new SvgRectangle
                {
                    Fill = new SvgColourServer(col),
                    FillOpacity = 1,
                    X = 0,
                    Y = _cycleWidth * _cores.Count,
                    Height = _cycleWidth,
                    Width = _startx + (_simulationlength * _cycleWidth)
                };

                _cores.Add(rectangles);
            }
            _cpus.Add(cpu);
            _texts.Add(cpuText);
        }
        public void AddTask(int from, int length, int cpu, int core, int taskId)
        {
            int offset = _coreoffset[cpu] + (core * _cycleWidth);
            SvgRectangle rectangles = new SvgRectangle
            {
                Fill = new SvgColourServer(_taskcolors[taskId % _taskcolors.Length]),
                FillOpacity = 1,
                X = _startx + (from * _cycleWidth),
                Y = offset,
                Height = _cycleWidth,
                Width = _cycleWidth * length
            };

            _cores.Add(rectangles);
        }
        public void Generate(string filename)
        {
            Createlines();
            _cores.ForEach(x => _document.Children.Add(x));
            _cpus.ForEach(x => _document.Children.Add(x));
            _lines.ForEach(x => _document.Children.Add(x));
            _texts.ForEach(x => _document.Children.Add(x));
            _document.Write(filename);
        }

        private void Createlines()
        {
            for (int idx = 0; idx < _simulationlength; idx++)
            {
                SvgLine line = new SvgLine()
                {
                    StartX = (idx * _cycleWidth) + _startx,
                    EndX = (idx * _cycleWidth) + _startx,
                    StartY = 0,
                    EndY = (_cores.Count * _cycleWidth) + 10,
                    StrokeWidth = 0.25F,
                    Stroke = new SvgColourServer(idx % 10 == 0 ? Color.Black : Color.FromArgb(255, 195, 195, 195)),
                };
                _lines.Add(line);
            }
        }
    }
}
