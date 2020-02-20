using System.Collections.Generic;
using System.Windows.Controls;
using System.Windows.Media;

namespace OMineWatcher.MVVM
{
    class DrawingCanvas : Canvas
    {
        public List<Visual> Visuals = new List<Visual>();
        protected override int VisualChildrenCount => Visuals.Count;
        protected override Visual GetVisualChild(int index) => Visuals[index];
        public void AddVisual(Visual visual)
        {
            Visuals.Add(visual);
            base.AddVisualChild(visual);
            base.AddLogicalChild(visual);
        }
        public void DeleteVisual(Visual visual)
        {
            Visuals.Remove(visual);
            base.RemoveVisualChild(visual);
            base.RemoveLogicalChild(visual);
        }
        public void AddVisualsRange(List<Visual> visuals)
        {
            visuals.AddRange(visuals);
            foreach (Visual v in visuals)
            {
                base.AddVisualChild(v);
                base.AddLogicalChild(v);
            }
        }
        public void ClearVisuals()
        {
            foreach (Visual v in Visuals)
            {
                base.RemoveVisualChild(v);
                base.RemoveLogicalChild(v);
            }
            Visuals.Clear();
        }

        public DrawingCanvas()
        {
        }
    }
}