using System;
using System.Windows;
using System.Windows.Controls;

namespace Up2dateConsole.Controls
{
    /// <summary>
    /// When used as a wrapper this control squeezes the child to the actually required space 
    /// preventing the child to occupy all available space if that is not needed.
    /// 
    /// This control doesn't have any visible elements.
    /// </summary>
    public class SqueezeContentControl : ContentControl
    {
        private Size squeezedSize;

        protected override Size MeasureOverride(Size constraint)
        {
            UIElement child = (UIElement)Content;
            child.Measure(constraint);
            squeezedSize.Width = Math.Min(child.DesiredSize.Width, constraint.Width);
            squeezedSize.Height = Math.Min(child.DesiredSize.Height, constraint.Height);

            return base.MeasureOverride(constraint);
        }

        protected override Size ArrangeOverride(Size arrangeBounds)
        {
            return base.ArrangeOverride(squeezedSize);
        }
    }
}
