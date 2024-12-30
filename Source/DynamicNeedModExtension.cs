using Verse;
namespace MindMatters;

public class DynamicNeedModExtension : DefModExtension
{
    public DynamicNeedCategory Category = DynamicNeedCategory.Secondary; // Default
    public DynamicNeedsBitmap? BitmapOverride; // Optional
}