// <developer>don@donxml.com</developer>
// <completed>100</completed>

using SharpVectors.Dom.Events;

namespace SharpVectors.Dom.Svg
{
    /// <summary>
    /// The SvgAElement interface corresponds to the 'a' element. 
    /// </summary>
    public interface ISvgAElement : ISvgElement, ISvgUriReference, ISvgTests,
		ISvgLangSpace, ISvgExternalResourcesRequired, ISvgStylable, 
        ISvgTransformable, IEventTarget, IElementVisitorTarget
    {
		ISvgAnimatedString Target{get;}
	}
}
