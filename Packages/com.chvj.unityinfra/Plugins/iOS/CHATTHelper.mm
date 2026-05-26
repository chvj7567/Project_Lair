#import <Foundation/Foundation.h>
#import <AppTrackingTransparency/AppTrackingTransparency.h>
#import <AdSupport/AdSupport.h>

// iOS 14+ App Tracking Transparency 권한 요청.
// Unity 측 CHMAdmob에서 UMP 진입 전에 호출. Info.plist에 NSUserTrackingUsageDescription 키 필수.
extern "C" {
    void CHATTRequestAuthorization() {
        if (@available(iOS 14, *)) {
            [ATTrackingManager requestTrackingAuthorizationWithCompletionHandler:^(ATTrackingManagerAuthorizationStatus status) {
                NSLog(@"[CHATT] tracking authorization status=%lu", (unsigned long)status);
            }];
        }
    }
}
