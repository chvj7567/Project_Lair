namespace ChvjUnityInfra
{
    /// <summary>구매 결과 상태. <c>purchaseState</c> 콜백으로 전달된다.</summary>
    public enum EPurchase
    {
        /// <summary>구매 성공.</summary>
        Success,
        /// <summary>구매 실패 (네트워크/유저 취소/플랫폼 오류 등).</summary>
        Failure,
        /// <summary>초기화 실패. Init 자체가 실패해 구매를 시도조차 못 한 경우.</summary>
        InitFailed,
        /// <summary>구매 복구 성공 (iOS/OSX RestoreTransactions).</summary>
        Restored,
        /// <summary>구매 복구 실패.</summary>
        RestoreFailed,
        /// <summary>Init이 끝나지 않아 구매를 시도할 수 없음.</summary>
        NotReady,
    }
}
