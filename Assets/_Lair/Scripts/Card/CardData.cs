using System;
using Lair.Data;
using UnityEngine;

namespace Lair.Card
{
    //# 카드 한 장 — SO 는 _id·그림만 들고, 축·이름·설명·효과는 Card.json 행(CardDatabase.Current)에서 처음 읽을 때 풀어 캐시한다.
    //# 행이 없는 카드는 LogError 후 HasData=false (풀 소비처가 후보에서 제외, so-to-json-migration §9 E5).
    [CreateAssetMenu(fileName = "Card_", menuName = "Lair/Card", order = 0)]
    public class CardData : ScriptableObject
    {
        [SerializeField] private ECardId _id;
        //# 빌드 패널 아이콘 — LairCardPrefabBuilder 가 ECardId 이름 PNG 로 배정. 없으면 null.
        [SerializeField] private Sprite _icon;

        //# 비직렬화 캐시 — DB 가 바뀌면(재로드·리셋) 다시 푼다. _injected 는 테스트 헬퍼(FakeCardData)가 값을 고정할 때만 켠다.
        private CardDatabase _db;
        private CardEntry _entry;
        private ICardEffect _effect;
        private bool _missingLogged;
        private bool _injected;

        public ECardId Id => _id;
        public Sprite Icon => _icon;

        //# Card.json 에 이 카드의 행이 있어 조회 값이 유효한가.
        public bool HasData => Entry() != null;

        public EBuildAxis Axis
        {
            get
            {
                CardEntry entry = Entry();
                return entry != null ? entry.Axis : default;
            }
        }

        public string DisplayName
        {
            get
            {
                CardEntry entry = Entry();
                return entry != null ? entry.DisplayName : string.Empty;
            }
        }

        public string Description
        {
            get
            {
                CardEntry entry = Entry();
                return entry != null ? entry.Description : string.Empty;
            }
        }

        public ICardEffect Effect
        {
            get
            {
                Entry();
                return _effect;
            }
        }

        private CardEntry Entry()
        {
            if (_injected)
                return _entry;

            CardDatabase db = CardDatabase.Current;
            if (db == _db)
                return _entry;

            _db = db;
            _entry = db != null ? db.GetCard(_id) : null;
            _effect = _entry != null ? CreateEffect(_entry) : null;
            if (db != null && _entry == null && _missingLogged == false)
            {
                _missingLogged = true;
                Debug.LogError($"[CardData] Card.json 에 {_id} 행이 없어 이 카드를 후보에서 제외합니다.");
            }
            return _entry;
        }

        //# ECardEffect → 효과 클래스 (so-to-json-migration §4.9). 새 효과는 여기 한 줄 + 클래스(수치 필드명 = paramKey).
        public static ICardEffect CreateEffect(CardEntry entry)
        {
            CardParams p = entry.Params;
            switch (entry.Effect)
            {
                case ECardEffect.WispHpBoost: return new WispHpBoostEffect(p);
                case ECardEffect.WraithDamageBoost: return new WraithDamageBoostEffect(p);
                case ECardEffect.ReaperAtkSpeed: return new ReaperAtkSpeedEffect(p);
                case ECardEffect.HexRangeBoost: return new HexRangeBoostEffect(p);
                case ECardEffect.PlagueToxinBoost: return new PlagueToxinBoostEffect(p);
                case ECardEffect.PhantomMoveSpeedBoost: return new PhantomMoveSpeedBoostEffect(p);
                case ECardEffect.SpawnWisps: return new SpawnWispsEffect();
                case ECardEffect.SpawnWraith: return new SpawnWraithEffect();
                case ECardEffect.SpawnReapers: return new SpawnReapersEffect();
                case ECardEffect.SpawnPlagues: return new SpawnPlaguesEffect();
                case ECardEffect.SpawnPhantoms: return new SpawnPhantomsEffect();
                case ECardEffect.WispWraithPowerBoost: return new WispWraithPowerBoostEffect(p);
                case ECardEffect.ReaperHexPowerBoost: return new ReaperHexPowerBoostEffect(p);
                case ECardEffect.HeroAttackDown: return new HeroAttackDownEffect();
                case ECardEffect.Fear: return new FearEffect(p);
                case ECardEffect.Bleed: return new BleedEffect(p);
                case ECardEffect.Weaken: return new WeakenEffect(p);
                case ECardEffect.Slow: return new SlowEffect(p);
                case ECardEffect.Frenzy: return new FrenzyEffect(p);
                case ECardEffect.FastBreeding: return new FastBreedingEffect(p);
                case ECardEffect.BloodThirst: return new BloodThirstEffect(p);
                case ECardEffect.IronWill: return new IronWillEffect(p);
                case ECardEffect.TimeStop: return new TimeStopEffect(p);
                case ECardEffect.GuardianRage: return new GuardianRageEffect(p);
                case ECardEffect.ToughHide: return new ToughHideEffect();
                case ECardEffect.MarkOfDeath: return new MarkOfDeathEffect(p);
                case ECardEffect.SpawnerHaste: return new SpawnerHasteEffect(p);
                case ECardEffect.LingeringCurse: return new LingeringCurseEffect(p);
                default: throw new ArgumentOutOfRangeException(nameof(entry), entry.Effect, "CardData.CreateEffect 에 없는 효과");
            }
        }
    }
}
