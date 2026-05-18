using TMPro;
using UnityEngine;

namespace ChvjUnityInfra
{
    public interface IFontProvider
    {
        TMP_FontAsset GetFont();
        Material GetFontMaterial();
    }
}
