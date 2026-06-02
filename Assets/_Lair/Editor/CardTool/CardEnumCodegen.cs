using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace Lair.EditorTools
{
    //# ECardId.cs 텍스트에 신규 카드 ID 를 append 하는 codegen. pure 함수는 단위 테스트 대상.
    public static class CardEnumCodegen
    {
        public const string Marker = "//# <card-editor:insert>";
        public const string EnumFilePath = "Assets/_Lair/Scripts/Data/ECardId.cs";
        private static readonly Regex IdentifierRx = new Regex(@"^[A-Za-z_][A-Za-z0-9_]*$");

        //# fileText 의 마커 줄 바로 위에 "        {newId}," 한 줄을 삽입한 새 텍스트를 반환한다.
        //# 잘못된 식별자/중복/마커없음이면 예외 — 파일을 절대 깨지 않는다.
        public static string InsertCardId(string fileText, string newId)
        {
            if (string.IsNullOrEmpty(newId) || IdentifierRx.IsMatch(newId) == false)
                throw new ArgumentException($"유효한 C# 식별자가 아님: '{newId}'");

            if (ContainsMember(fileText, newId))
                throw new ArgumentException($"이미 존재하는 ECardId: '{newId}'");

            int markerIdx = fileText.IndexOf(Marker, StringComparison.Ordinal);
            if (markerIdx < 0)
                throw new InvalidOperationException($"삽입 마커({Marker})를 찾지 못함 — ECardId.cs 구조 확인 필요");

            //# 마커가 있는 줄의 시작 위치
            int lineStart = fileText.LastIndexOf('\n', markerIdx) + 1;
            string indent = fileText.Substring(lineStart, markerIdx - lineStart);
            string insertion = $"{indent}{newId},\n";
            return fileText.Insert(lineStart, insertion);
        }

        //# 실제 ECardId.cs 에 신규 ID 를 append 하고 컴파일을 트리거한다.
        //# 성공 시 true. 실패(예외) 시 파일 미변경 + false, 에러 다이얼로그.
        public static bool AppendCardId(string newId)
        {
            try
            {
                string text = File.ReadAllText(EnumFilePath, Encoding.UTF8);
                string updated = InsertCardId(text, newId);
                File.WriteAllText(EnumFilePath, updated, new UTF8Encoding(false));
                AssetDatabase.ImportAsset(EnumFilePath, ImportAssetOptions.ForceUpdate);
                AssetDatabase.Refresh();
                Debug.Log($"[CardEnumCodegen] ECardId 추가: {newId} — 재컴파일 대기");
                return true;
            }
            catch (Exception e)
            {
                EditorUtility.DisplayDialog("Enum 추가 실패", e.Message, "확인");
                return false;
            }
        }

        //# enum 멤버로 이미 선언된 식별자인지 (주석/타 토큰 무시, 멤버 라인 패턴 매칭).
        private static bool ContainsMember(string fileText, string id)
        {
            Regex memberRx = new Regex($@"(?m)^\s*{Regex.Escape(id)}\s*(,|=|$)");
            return memberRx.IsMatch(fileText);
        }
    }
}
