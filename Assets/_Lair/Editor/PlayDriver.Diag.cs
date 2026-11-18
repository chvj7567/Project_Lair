using System.IO;
using System.Text;
using ChvjUnityInfra;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Lair.EditorTools
{
    //# Play 중 DailyPopup 의 TMP 라벨 렌더 상태 덤프 — 라벨이 안 그려지는 원인 진단용. 인자 없음.
    public static partial class PlayDriver
    {
        private const string DiagPath = "Temp/PlayDriverDiag.txt";
        private const string DailyRootName = "DailyPopup(Clone)";

        public static string DumpDailyLabels()
        {
            if (EditorApplication.isPlaying == false)
                return NotPlaying;

            TMP_Text[] all = Object.FindObjectsByType<TMP_Text>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            StringBuilder sb = new StringBuilder();
            bool found = false;
            TMP_FontAsset headerFont = null;
            StringBuilder lines = new StringBuilder();

            for (int i = 0; i < all.Length; i++)
            {
                TMP_Text tmp = all[i];
                string path = DiagPath_Of(tmp.transform);
                if (path.Contains(DailyRootName) == false)
                    continue;

                found = true;
                if (headerFont == null)
                    headerFont = tmp.font;

                lines.AppendLine(DiagLine(tmp, path));
            }

            if (found == false)
                return "NO DAILYPOPUP";

            sb.AppendLine(DiagFontLine(headerFont));
            sb.AppendLine(DiagStringLine());
            sb.Append(lines.ToString());

            string result = sb.ToString();
            File.WriteAllText(DiagPath, result, new UTF8Encoding(false));
            return result;
        }

        private static string DiagPath_Of(Transform t)
        {
            StringBuilder p = new StringBuilder(t.name);
            for (Transform cur = t.parent; cur != null; cur = cur.parent)
            {
                p.Insert(0, cur.name + "/");
            }
            return p.ToString();
        }

        private static string DiagFontLine(TMP_FontAsset font)
        {
            if (font == null)
                return "FONT null";

            StringBuilder sb = new StringBuilder();
            sb.Append("FONT ").Append(font.name)
              .Append(" atlasTextures=").Append(font.atlasTextures == null ? -1 : font.atlasTextures.Length)
              .Append(" popMode=").Append(font.atlasPopulationMode)
              .Append(" glyphTable=").Append(font.glyphTable.Count)
              .Append(" characterTable=").Append(font.characterTable.Count);
            if (font.atlasTextures != null)
            {
                for (int i = 0; i < font.atlasTextures.Length; i++)
                {
                    Texture2D tex = font.atlasTextures[i];
                    if (tex == null)
                    {
                        sb.Append(" [atlas").Append(i).Append(" null]");
                        continue;
                    }
                    sb.Append(" [atlas").Append(i).Append(" readable=").Append(tex.isReadable)
                      .Append(" ").Append(tex.width).Append("x").Append(tex.height).Append("]");
                }
            }
            return sb.ToString();
        }

        private static string DiagStringLine()
        {
            if (CHText.StringProvider == null)
                return "STRINGS provider=null";

            int[] ids = { 1414, 1421, 1437, 1438, 1439, 1440, 1441, 1442, 1443, 1444, 1445, 1446, 1447, 1448, 1449, 1450 };
            StringBuilder sb = new StringBuilder("STRINGS");
            for (int i = 0; i < ids.Length; i++)
            {
                string s = CHText.StringProvider.GetString(ids[i]);
                sb.Append(" ").Append(ids[i]).Append(string.IsNullOrEmpty(s) ? "=EMPTY" : "=ok");
            }
            return sb.ToString();
        }

        private static string DiagLine(TMP_Text tmp, string path)
        {
            StringBuilder sb = new StringBuilder();
            string text = tmp.text ?? string.Empty;
            if (text.Length > 30)
                text = text.Substring(0, 30);
            text = text.Replace("\n", "\\n");

            int before = tmp.textInfo != null ? tmp.textInfo.characterCount : -1;
            tmp.ForceMeshUpdate();
            int after = tmp.textInfo != null ? tmp.textInfo.characterCount : -1;

            int vertexCount = -1;
            if (tmp.textInfo != null && tmp.textInfo.meshInfo != null && tmp.textInfo.meshInfo.Length > 0)
                vertexCount = tmp.textInfo.meshInfo[0].vertexCount;

            int subMeshes = tmp.GetComponentsInChildren<TMP_SubMeshUI>(true).Length;
            Material mat = tmp.fontSharedMaterial;

            sb.Append(path)
              .Append(" | active=").Append(tmp.gameObject.activeInHierarchy)
              .Append(" text='").Append(text).Append("'")
              .Append(" size=").Append(tmp.fontSize)
              .Append(" color=").Append(tmp.color)
              .Append(" font=").Append(tmp.font != null ? tmp.font.name : "null")
              .Append(" mat=").Append(mat != null ? mat.name + "#" + mat.GetInstanceID() : "null")
              .Append(" chars(before/after)=").Append(before).Append("/").Append(after)
              .Append(" verts0=").Append(vertexCount)
              .Append(" subMeshes=").Append(subMeshes);

            CanvasRenderer cr = tmp.canvasRenderer;
            if (cr != null)
            {
                sb.Append(" cr[cull=").Append(cr.cull)
                  .Append(" cullTransparent=").Append(cr.cullTransparentMesh)
                  .Append(" alpha=").Append(cr.GetAlpha())
                  .Append(" inheritedAlpha=").Append(cr.GetInheritedAlpha())
                  .Append(" depth=").Append(cr.absoluteDepth).Append("]");
            }
            else
            {
                sb.Append(" cr=null");
            }

            Vector3[] corners = new Vector3[4];
            tmp.rectTransform.GetWorldCorners(corners);
            sb.Append(" rect=").Append(tmp.rectTransform.rect)
              .Append(" world[bl=").Append(corners[0]).Append(" tr=").Append(corners[2]).Append("]");

            bool hasGroup = false;
            float groupAlpha = 1f;
            bool ignoreParents = false;
            bool hasMask = false;
            bool hasRectMask = false;
            for (Transform cur = tmp.transform; cur != null; cur = cur.parent)
            {
                CanvasGroup group = cur.GetComponent<CanvasGroup>();
                if (group != null)
                {
                    hasGroup = true;
                    groupAlpha *= group.alpha;
                    ignoreParents = ignoreParents || group.ignoreParentGroups;
                }
                if (cur.GetComponent<Mask>() != null)
                    hasMask = true;
                if (cur.GetComponent<RectMask2D>() != null)
                    hasRectMask = true;
            }
            sb.Append(" chain[canvasGroup=").Append(hasGroup)
              .Append(" alphaProduct=").Append(groupAlpha)
              .Append(" ignoreParentGroups=").Append(ignoreParents)
              .Append(" mask=").Append(hasMask)
              .Append(" rectMask2D=").Append(hasRectMask).Append("]");

            sb.Append(" truncated=").Append(tmp.isTextTruncated)
              .Append(" rendered=").Append(tmp.renderedWidth).Append("x").Append(tmp.renderedHeight)
              .Append(" preferredW=").Append(tmp.preferredWidth);
            return sb.ToString();
        }
    }
}
