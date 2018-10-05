﻿using SharpVectors.Dom.Css;
using SharpVectors.Dom.Svg;
using System;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Media;

namespace SharpVectors.Renderers.Wpf.Shape
{
    public static class WpfHelper
    {
        private static readonly Regex _decimalNumber = new Regex(@"^\d");
        private static readonly string GenericSerifFontFamily = "Times New Roman";
        private static readonly string GenericSansSerifFontFamily = "Tahoma";
        private static readonly string GenericMonospaceFontFamily = "MS Gothic";
        private static readonly string DefaultFontFamily = "Arial Unicode MS";

        public static bool TryGetStrokeWidth(SvgStyleableElement element, out double strokeWidth)
        {
            string propValue = element.GetPropertyValue("stroke-width");
            if (string.IsNullOrEmpty(propValue))
            {
                strokeWidth = 1d;
                return false;
            }

            SvgLength strokeWidthLength = new SvgLength(element, "stroke-width", SvgLengthDirection.Viewport, propValue);
            strokeWidth = strokeWidthLength.Value;
            return true;
        }

        public static bool TryGetMiterLimit(SvgStyleableElement element, double strokeWidth, out double miterLimit)
        {
            string miterLimitAttr = element.GetAttribute("stroke-miterlimit");
            if (String.IsNullOrEmpty(miterLimitAttr))
            {
                string strokeLinecap = element.GetAttribute("stroke-linecap");
                if (String.Equals(strokeLinecap, "round", StringComparison.OrdinalIgnoreCase))
                {
                    miterLimit = 1.0d;
                    return true;
                }
                miterLimit = -1.0d;
                return false;
            }

            string miterLimitStr = element.GetPropertyValue("stroke-miterlimit");
            if (String.IsNullOrEmpty(miterLimitStr) || strokeWidth <= 0)
            {
                miterLimit = -1.0d;
                return false;
            }

            miterLimit = SvgNumber.ParseNumber(miterLimitStr);
            if (miterLimit < 1)
                return false;

            double ratioLimit = miterLimit / strokeWidth;
            if (ratioLimit < 1.8d)
            {
                miterLimit = 1.0d;
            }
            return true;
        }

        public static bool TryGetFillRule(SvgStyleableElement element, out FillRule fillRule)
        {
            string fillRuleStr = element.GetPropertyValue("fill-rule");
            string clipRule = element.GetAttribute("clip-rule");
            if (!String.IsNullOrEmpty(clipRule) &&
                String.Equals(clipRule, "evenodd") || String.Equals(clipRule, "nonzero"))
            {
                fillRuleStr = clipRule;
            }
            if (StringComparer.InvariantCultureIgnoreCase.Equals(fillRuleStr, "evenodd"))
            {
                fillRule = FillRule.EvenOdd;
                return true;
            }
            else if (StringComparer.InvariantCultureIgnoreCase.Equals(fillRuleStr, "nonzero"))
            {
                fillRule = FillRule.Nonzero;
                return true;
            }
            fillRule = FillRule.EvenOdd;
            return false;
        }

        public static bool TryGetSpreadMethod(SvgSpreadMethod sm, out GradientSpreadMethod spreadMethod)
        {
            if (sm == SvgSpreadMethod.None)
            {
                spreadMethod = GradientSpreadMethod.Pad;
                return false;
            }

            spreadMethod = (GradientSpreadMethod)sm;
            return true;
        }

        public static Matrix ToWpfMatrix(ISvgMatrix svgMatrix)
        {
            return new Matrix(svgMatrix.A, svgMatrix.B, svgMatrix.C,
                svgMatrix.D, svgMatrix.E, svgMatrix.F);
        }

        public static bool TryGetLineJoin(SvgStyleableElement element, out PenLineJoin lineJoin)
        {
            switch (element.GetPropertyValue("stroke-linejoin"))
            {
                case "round":
                    lineJoin = PenLineJoin.Round;
                    return true;
                case "bevel":
                    lineJoin = PenLineJoin.Bevel;
                    return true;
                case "miter":
                    lineJoin = PenLineJoin.Miter;
                    return true;
            }
            lineJoin = PenLineJoin.Bevel;
            return false;
        }

        public static bool TryGetLineCap(SvgStyleableElement element, out PenLineCap lineCap)
        {
            switch (element.GetPropertyValue("stroke-linecap"))
            {
                case "round":
                    lineCap = PenLineCap.Round;
                    return true;
                case "square":
                    lineCap = PenLineCap.Square;
                    return true;
                case "butt":
                    lineCap = PenLineCap.Flat;
                    return true;
                case "triangle":
                    lineCap = PenLineCap.Triangle;
                    return true;
            }
            lineCap = PenLineCap.Flat;
            return false;
        }

        public static bool TryGetDashArray(SvgStyleableElement element, double strokeWidth, out DoubleCollection dashArray)
        {
            dashArray = null;
            string dashArrayText = element.GetPropertyValue("stroke-dasharray");
            if (String.IsNullOrEmpty(dashArrayText))
            {
                return false;
            }

            if (dashArrayText == "none")
            {
                return false;
            }
            else
            {
                SvgNumberList list = new SvgNumberList(dashArrayText);

                uint len = list.NumberOfItems;
                dashArray = new DoubleCollection((int)len);

                for (uint i = 0; i < len; i++)
                {
                    //divide by strokeWidth to take care of the difference between Svg and WPF
                    dashArray.Add(list.GetItem(i).Value / strokeWidth);
                }

                return true;
            }
        }

        public static bool TryGetDashOffset(SvgStyleableElement element, double strokeWidth, out double offset)
        {
            string dashOffset = element.GetPropertyValue("stroke-dashoffset");
            if (dashOffset.Length > 0)
            {
                //divide by strokeWidth to take care of the difference between Svg and GDI+
                SvgLength dashOffsetLength = new SvgLength(element, "stroke-dashoffset",
                    SvgLengthDirection.Viewport, dashOffset);
                offset = dashOffsetLength.Value;
                return true;
            }
            offset = 0;
            return false;
        }

        public static bool TryGetBrush(SvgStyleableElement element, string property, Rect bounds, Matrix transform, out Brush brush)
        {
            SvgPaint paint = new SvgPaint(element.GetComputedStyle("").GetPropertyValue(property));
            SvgPaint svgBrush;
            if (paint.PaintType == SvgPaintType.None)
            {
                brush = null;
                return false;
            }
            else if (paint.PaintType == SvgPaintType.CurrentColor)
            {
                svgBrush = new SvgPaint(element.GetComputedStyle("").GetPropertyValue("color"));
            }
            else
            {
                svgBrush = paint;
            }

            SvgPaintType paintType = svgBrush.PaintType;
            if (paintType == SvgPaintType.Uri || paintType == SvgPaintType.UriCurrentColor ||
                paintType == SvgPaintType.UriNone || paintType == SvgPaintType.UriRgbColor ||
                paintType == SvgPaintType.UriRgbColorIccColor)
            {
                SvgStyleableElement fillNode = null;
                string absoluteUri = element.ResolveUri(svgBrush.Uri);

                if (element.Imported && element.ImportDocument != null &&
                    element.ImportNode != null)
                {
                    // We need to determine whether the provided URI refers to element in the
                    // original document or in the current document...
                    SvgStyleableElement styleElm = element.ImportNode as SvgStyleableElement;
                    if (styleElm != null)
                    {
                        string propertyValue = styleElm.GetComputedStyle("").GetPropertyValue(property);

                        if (!String.IsNullOrEmpty(propertyValue))
                        {
                            SvgPaint importFill = new SvgPaint(styleElm.GetComputedStyle("").GetPropertyValue(property));
                            if (String.Equals(svgBrush.Uri, importFill.Uri, StringComparison.OrdinalIgnoreCase))
                            {
                                fillNode = element.ImportDocument.GetNodeByUri(absoluteUri) as SvgStyleableElement;
                            }
                        }
                    }
                }
                else
                {
                    fillNode = element.OwnerDocument.GetNodeByUri(absoluteUri) as SvgStyleableElement;
                }

                if (fillNode != null)
                {
                    switch (fillNode)
                    {
                        case SvgLinearGradientElement linearGradient:
                            brush = ConstructBrush(linearGradient, bounds, transform);
                            return true;
                        case SvgRadialGradientElement radialGradient:
                            brush = ConstructBrush(radialGradient, bounds, transform);
                            return true;
                        case SvgPatternElement pattern:
                            brush = ConstructBrush(pattern, bounds, transform);
                            return true;
                    }
                }
            }

            if (svgBrush == null || svgBrush.RgbColor == null ||
                !TryConvertColor(svgBrush.RgbColor, out Color solidColor))
            {
                brush = null;
                return false;
            }

            brush = new SolidColorBrush(solidColor);
            brush.Opacity = GetOpacity(element, property);
            if (brush.CanFreeze)
                brush.Freeze();
            return true;
        }

        public static LinearGradientBrush ConstructBrush(SvgLinearGradientElement gradient, Rect bounds, Matrix transform)
        {
            if (gradient.Stops.Count == 0)
                return null;

            double x1 = gradient.X1.AnimVal.Value;
            double x2 = gradient.X2.AnimVal.Value;
            double y1 = gradient.Y1.AnimVal.Value;
            double y2 = gradient.Y2.AnimVal.Value;

            GradientStopCollection gradientStops = ToGradientStops(gradient.Stops);

            LinearGradientBrush brush = new LinearGradientBrush(gradientStops,
                new Point(x1, y1), new Point(x2, y2));

            SvgSpreadMethod spreadMethod = SvgSpreadMethod.Pad;
            if (gradient.SpreadMethod != null)
            {
                spreadMethod = (SvgSpreadMethod)gradient.SpreadMethod.AnimVal;
                if (TryGetSpreadMethod(spreadMethod, out GradientSpreadMethod sm))
                {
                    brush.SpreadMethod = sm;
                }
            }

            SvgUnitType mappingMode = SvgUnitType.ObjectBoundingBox;
            if (gradient.GradientUnits != null)
            {
                mappingMode = (SvgUnitType)gradient.GradientUnits.AnimVal;
                if (mappingMode == SvgUnitType.ObjectBoundingBox)
                {
                    brush.MappingMode = BrushMappingMode.RelativeToBoundingBox;
                }
                else if (mappingMode == SvgUnitType.UserSpaceOnUse)
                {
                    brush.MappingMode = BrushMappingMode.Absolute;
                    brush.StartPoint.Offset(bounds.X, bounds.Y);
                    brush.EndPoint.Offset(bounds.X, bounds.Y);
                }
            }

            Matrix brushTransform = ToWpfMatrix(((SvgTransformList)gradient.GradientTransform.AnimVal).TotalMatrix);
            if (mappingMode == SvgUnitType.UserSpaceOnUse)
            {
                brushTransform *= transform;
            }
            if (!brushTransform.IsIdentity)
            {
                brush.Transform = new MatrixTransform(brushTransform);
            }

            string colorInterpolation = gradient.GetPropertyValue("color-interpolation");
            if (!String.IsNullOrEmpty(colorInterpolation))
            {
                if (colorInterpolation == "linearRGB")
                {
                    brush.ColorInterpolationMode = ColorInterpolationMode.ScRgbLinearInterpolation;
                }
                else
                {
                    brush.ColorInterpolationMode = ColorInterpolationMode.SRgbLinearInterpolation;
                }
            }

            return brush;
        }

        public static RadialGradientBrush ConstructBrush(SvgRadialGradientElement gradient, Rect bounds, Matrix transform)
        {
            if (gradient.Stops.Count == 0)
                return null;

            double centerX = gradient.Cx.AnimVal.Value;
            double centerY = gradient.Cy.AnimVal.Value;
            double focusX = gradient.Fx.AnimVal.Value;
            double focusY = gradient.Fy.AnimVal.Value;
            double radius = gradient.R.AnimVal.Value;

            GradientStopCollection gradientStops = ToGradientStops(gradient.Stops);

            RadialGradientBrush brush = new RadialGradientBrush(gradientStops);

            brush.RadiusX = radius;
            brush.RadiusY = radius;
            brush.Center = new Point(centerX, centerY);
            brush.GradientOrigin = new Point(focusX, focusY);

            if (gradient.SpreadMethod != null)
            {
                SvgSpreadMethod spreadMethod = (SvgSpreadMethod)gradient.SpreadMethod.AnimVal;
                if (TryGetSpreadMethod(spreadMethod, out GradientSpreadMethod sm))
                {
                    brush.SpreadMethod = sm;
                }
            }

            SvgUnitType mappingMode = SvgUnitType.ObjectBoundingBox;
            if (gradient.GradientUnits != null)
            {
                mappingMode = (SvgUnitType)gradient.GradientUnits.AnimVal;
                if (mappingMode == SvgUnitType.ObjectBoundingBox)
                {
                    brush.MappingMode = BrushMappingMode.RelativeToBoundingBox;
                }
                else if (mappingMode == SvgUnitType.UserSpaceOnUse)
                {
                    brush.MappingMode = BrushMappingMode.Absolute;
                    brush.Center.Offset(bounds.X, bounds.Y);
                    brush.GradientOrigin.Offset(bounds.X, bounds.Y);
                }
            }

            Matrix brushTransform = ToWpfMatrix(((SvgTransformList)gradient.GradientTransform.AnimVal).TotalMatrix);
            if (mappingMode == SvgUnitType.UserSpaceOnUse)
            {
                brushTransform *= transform;
            }
            if (!brushTransform.IsIdentity)
            {
                brush.Transform = new MatrixTransform(brushTransform);
            }

            string colorInterpolation = gradient.GetPropertyValue("color-interpolation");
            if (!String.IsNullOrEmpty(colorInterpolation))
            {
                if (colorInterpolation == "linearRGB")
                {
                    brush.ColorInterpolationMode = ColorInterpolationMode.SRgbLinearInterpolation;
                }
                else
                {
                    brush.ColorInterpolationMode = ColorInterpolationMode.ScRgbLinearInterpolation;
                }
            }

            return brush;
        }

        public static ImageBrush ConstructBrush(SvgPatternElement linearGradient, Rect bounds, Matrix transform)
        {
            return null;
        }

        public static GradientStopCollection ToGradientStops(System.Xml.XmlNodeList stops)
        {
            int itemCount = stops.Count;
            GradientStopCollection gradientStops = new GradientStopCollection(itemCount);

            double lastOffset = 0;
            for (int i = 0; i < itemCount; i++)
            {
                SvgStopElement stop = (SvgStopElement)stops.Item(i);
                string prop = stop.GetAttribute("stop-color");
                string style = stop.GetAttribute("style");
                Color color = Colors.Transparent; // no auto-inherited...
                if (!String.IsNullOrEmpty(prop) || !String.IsNullOrEmpty(style))
                {
                    SvgColor svgColor = new SvgColor(stop.GetComputedStyle("").GetPropertyValue("stop-color"));
                    if (svgColor.ColorType == SvgColorType.CurrentColor)
                    {
                        string sCurColor = stop.GetComputedStyle("").GetPropertyValue("color");
                        svgColor = new SvgColor(sCurColor);
                    }
                    TryConvertColor(svgColor.RgbColor, out color);
                }
                else
                {
                    color = Colors.Black; // the default color...
                }

                double alpha = 255;
                string opacity;

                opacity = stop.GetAttribute("stop-opacity"); // no auto-inherit
                if (opacity == "inherit") // if explicitly defined...
                {
                    opacity = stop.GetPropertyValue("stop-opacity");
                }
                if (!String.IsNullOrEmpty(opacity))
                    alpha *= SvgNumber.ParseNumber(opacity);

                alpha = Math.Min(alpha, 255);
                alpha = Math.Max(alpha, 0);

                color = Color.FromArgb((byte)Convert.ToInt32(alpha),
                    color.R, color.G, color.B);

                double offset = stop.Offset.AnimVal;

                offset /= 100;
                offset = Math.Max(lastOffset, offset);

                gradientStops.Add(new GradientStop(color, offset));
                lastOffset = offset;
            }

            return gradientStops;
        }

        public static bool TryConvertColor(ICssColor color, out Color wpfColor)
        {
            wpfColor = Colors.Black;
            if (color == null)
            {
                return false;
            }

            double dRed = color.Red.GetFloatValue(CssPrimitiveType.Number);
            double dGreen = color.Green.GetFloatValue(CssPrimitiveType.Number);
            double dBlue = color.Blue.GetFloatValue(CssPrimitiveType.Number);

            if (Double.IsNaN(dRed) || Double.IsInfinity(dRed) ||
                Double.IsNaN(dGreen) || Double.IsInfinity(dGreen) ||
                Double.IsNaN(dBlue) || Double.IsInfinity(dBlue))
                return false;

            wpfColor = Color.FromRgb(Convert.ToByte(dRed), Convert.ToByte(dGreen), Convert.ToByte(dBlue));
            return true;
        }

        public static double GetOpacity(SvgStyleableElement element, string fillOrStroke)
        {
            double opacityValue = 1;

            string opacity = element.GetPropertyValue(fillOrStroke + "-opacity");
            if (opacity != null && opacity.Length > 0)
            {
                opacityValue *= SvgNumber.ParseNumber(opacity);
            }

            opacity = element.GetPropertyValue("opacity");
            if (opacity != null && opacity.Length > 0)
            {
                opacityValue *= SvgNumber.ParseNumber(opacity);
            }

            opacityValue = Math.Min(opacityValue, 1);
            opacityValue = Math.Max(opacityValue, 0);

            return opacityValue;
        }

        public static bool TryGetTransform(ISvgTransformable element, out Transform transform)
        {
            transform = null;

            if (element == null)
                return false;

            ISvgTransformList svgTList = (ISvgTransformList)element.Transform.AnimVal;
            ISvgMatrix svgMatrix = ((SvgTransformList)element.Transform.AnimVal).TotalMatrix;
            ISvgElement nVE = element.NearestViewportElement;
            if (nVE != null)
            {
                SvgTransformableElement par = (element as SvgElement).ParentNode as SvgTransformableElement;
                while (par != null && par != nVE)
                {
                    svgTList = par.Transform.AnimVal;
                    svgMatrix = svgTList.Consolidate().Matrix.Multiply(svgMatrix);
                    par = par.ParentNode as SvgTransformableElement;
                }
            }

            if (svgMatrix.IsIdentity)
            {
                transform = Transform.Identity;
                return false;
            }

            transform = new MatrixTransform(ToWpfMatrix(svgMatrix));
            return true;
        }

        public static Geometry ConstructTextGeometry(SvgTextContentElement textContentElement, string text, Point position, out Size textDimensions)
        {
            Typeface typeface = new Typeface(
                GetTextFontFamily(textContentElement),
                GetTextFontStyle(textContentElement),
                GetTextFontWeight(textContentElement),
                GetTextFontStretch(textContentElement));

            FormattedText formattedText = new FormattedText(text,
                System.Globalization.CultureInfo.CurrentUICulture,
                GetTextDirection(textContentElement),
                typeface,
                GetComputedFontSize(textContentElement),
                Brushes.Black)
            {
                LineHeight = GetComputedLineHeight(textContentElement)
            };

            if (textContentElement is SvgTextPositioningElement tpe)
            {
                position = GetCurrentTextPosition(tpe, position);
            }
            position.Y -= formattedText.Height;

            textDimensions = new Size(formattedText.Width, formattedText.Height);
            return formattedText.BuildGeometry(position);
        }

        public static Point GetCurrentTextPosition(SvgTextPositioningElement posElement, Point p)
        {
            if (posElement.X.AnimVal.NumberOfItems > 0)
            {
                p.X = (float)posElement.X.AnimVal.GetItem(0).Value;
            }
            if (posElement.Y.AnimVal.NumberOfItems > 0)
            {
                p.Y = (float)posElement.Y.AnimVal.GetItem(0).Value;
            }
            if (posElement.Dx.AnimVal.NumberOfItems > 0)
            {
                p.X += (float)posElement.Dx.AnimVal.GetItem(0).Value;
            }
            if (posElement.Dy.AnimVal.NumberOfItems > 0)
            {
                p.Y += (float)posElement.Dy.AnimVal.GetItem(0).Value;
            }
            return p;
        }

        private static FlowDirection GetTextDirection(SvgTextContentElement element)
        {
            string dir = element.GetPropertyValue("direction");
            bool isRightToLeft = (dir == "rtl");
            return isRightToLeft ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;
        }

        private static double GetComputedFontSize(SvgTextContentElement element)
        {
            string str = element.GetPropertyValue("font-size");
            double fontSize = 12;
            if (str.EndsWith("%"))
            {
                // percentage of inherited value
            }
            else if (_decimalNumber.IsMatch(str))
            {
                // svg length
                fontSize = new SvgLength(element, "font-size",
                    SvgLengthDirection.Viewport, str, "10px").Value;
            }
            else if (str == "larger")
            {
            }
            else if (str == "smaller")
            {

            }
            else
            {
                // check for absolute value
            }

            return fontSize;
        }

        private static double GetComputedLineHeight(SvgTextContentElement element)
        {
            string str = element.GetPropertyValue("line-height");
            double lineHeight = 13;
            if (str.EndsWith("%"))
            {
                // percentage of inherited value
            }
            else if (_decimalNumber.IsMatch(str))
            {
                // svg length
                lineHeight = new SvgLength(element, "line-height",
                    SvgLengthDirection.Viewport, str, "13px").Value;
            }

            return lineHeight;
        }

        private static FontFamily GetTextFontFamily(SvgTextContentElement element)
        {
            string fontFamily = element.GetPropertyValue("font-family");
            string[] fontNames = fontNames = fontFamily.Split(new char[1] { ',' });

            foreach (string fn in fontNames)
            {
                try
                {
                    string fontName = fn.Trim(new char[] { ' ', '\'', '"' });

                    if (String.Equals(fontName, "serif", StringComparison.OrdinalIgnoreCase))
                    {
                        fontName = GenericSerifFontFamily;
                    }
                    else if (String.Equals(fontName, "sans-serif", StringComparison.OrdinalIgnoreCase))
                    {
                        fontName = GenericSansSerifFontFamily;
                    }
                    else if (String.Equals(fontName, "monospace", StringComparison.OrdinalIgnoreCase))
                    {
                        fontName = GenericMonospaceFontFamily;
                    }

                    if (!string.IsNullOrEmpty(fontName))
                        return new FontFamily(fontName);
                }
                catch
                {
                }
            }

            // no known font-family was found => default to Arial
            return new FontFamily(DefaultFontFamily);
        }

        private static FontStyle GetTextFontStyle(SvgTextContentElement element)
        {
            string fontStyle = element.GetPropertyValue("font-style");
            if (String.IsNullOrEmpty(fontStyle))
            {
                return FontStyles.Normal;
            }

            if (fontStyle == "normal")
            {
                return FontStyles.Normal;
            }
            if (fontStyle == "italic")
            {
                return FontStyles.Italic;
            }
            if (fontStyle == "oblique")
            {
                return FontStyles.Oblique;
            }

            return FontStyles.Normal;
        }

        private static FontStretch GetTextFontStretch(SvgTextContentElement element)
        {
            string fontStretch = element.GetPropertyValue("font-stretch");
            if (String.IsNullOrEmpty(fontStretch))
            {
                return FontStretches.Normal;
            }

            switch (fontStretch)
            {
                case "normal":
                    return FontStretches.Normal;
                case "ultra-condensed":
                    return FontStretches.UltraCondensed;
                case "extra-condensed":
                    return FontStretches.ExtraCondensed;
                case "condensed":
                    return FontStretches.Condensed;
                case "semi-condensed":
                    return FontStretches.SemiCondensed;
                case "semi-expanded":
                    return FontStretches.SemiExpanded;
                case "expanded":
                    return FontStretches.Expanded;
                case "extra-expanded":
                    return FontStretches.ExtraExpanded;
                case "ultra-expanded":
                    return FontStretches.UltraExpanded;
            }

            return FontStretches.Normal;
        }

        private static TextDecorationCollection GetTextDecoration(SvgTextContentElement element)
        {
            string textDeco = element.GetPropertyValue("text-decoration");
            if (textDeco == "line-through")
            {
                return TextDecorations.Strikethrough;
            }
            if (textDeco == "underline")
            {
                return TextDecorations.Underline;
            }
            if (textDeco == "overline")
            {
                return TextDecorations.OverLine;
            }

            return null;
        }

        private static FontWeight GetTextFontWeight(SvgTextContentElement element)
        {
            string fontWeight = element.GetPropertyValue("font-weight");
            if (String.IsNullOrEmpty(fontWeight))
            {
                return FontWeights.Normal;
            }

            switch (fontWeight)
            {
                case "normal":
                    return FontWeights.Normal;
                case "bold":
                    return FontWeights.Bold;
                case "100":
                    return FontWeights.Thin;
                case "200":
                    return FontWeights.ExtraLight;
                case "300":
                    return FontWeights.Light;
                case "400":
                    return FontWeights.Normal;
                case "500":
                    return FontWeights.Medium;
                case "600":
                    return FontWeights.SemiBold;
                case "700":
                    return FontWeights.Bold;
                case "800":
                    return FontWeights.ExtraBold;
                case "900":
                    return FontWeights.Black;
                case "950":
                    return FontWeights.UltraBlack;
            }

            return FontWeights.Normal;
        }
    }
}
