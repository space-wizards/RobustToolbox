#import <Cocoa/Cocoa.h>
#include <objc/runtime.h>

#include <cstdio>

#include "include/cef_application_mac.h"

// "inspired" by https://github.com/chromiumembedded/java-cef/blob/2caef5a994d85e86de670b35cf77950bcedba7bb/native/util_mac.mm
// And also just what I could find on the internet.

// This is global. There can only ever be one NSApplication anyways.
bool g_handling_send_event = false;

@interface NSApplication (RobustApplication) <CefAppProtocol>

- (BOOL)isHandlingSendEvent;
- (void)setHandlingSendEvent:(BOOL)handlingSendEvent;
- (void)_swizzled_sendEvent:(NSEvent*)event;

@end

@implementation NSApplication (RobustApplication)

+ (void)load {
    Method original = class_getInstanceMethod(self, @selector(sendEvent));
    Method swizzled = class_getInstanceMethod(self, @selector(_swizzled_sendEvent));
    method_exchangeImplementations(original, swizzled);
}

- (BOOL)isHandlingSendEvent {
    return g_handling_send_event;
}

- (void)setHandlingSendEvent:(BOOL)handlingSendEvent {
    printf("setHandlingSendEvent: %i\n", (int) handlingSendEvent);
    g_handling_send_event = handlingSendEvent;
}

- (void)_swizzled_sendEvent:(NSEvent*)event {
    printf("send it");
    CefScopedSendingEvent sendingEventScoper;
    [self _swizzled_sendEvent:event];
}

@end

extern "C" {
    void get_swizzled_idiot() {
        NSApplication* app = [NSApplication sharedApplication];
        [app isHandlingSendEvent];
    }
}
