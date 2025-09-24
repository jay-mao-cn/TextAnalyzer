#import <Cocoa/Cocoa.h>

// Declare a callback function pointer type for managed interop
typedef void (*OpenFilesCallback)(int count, const char* files[]);

@interface MacOpenFileDelegate : NSObject <NSApplicationDelegate>
@property OpenFilesCallback callback;
@end

@implementation MacOpenFileDelegate
- (BOOL)application:(NSApplication *)sender openFile:(NSString *)filename {
    if (self.callback) {
        const char* cstr = [filename UTF8String];
        const char* files[1] = { cstr };
        self.callback(1, files);
    }
    return YES;
}

- (void)application:(NSApplication *)sender openFiles:(NSArray<NSString *> *)filenames {
    if (self.callback) {
        int count = (int)[filenames count];
        const char* files[count];
        for (int i = 0; i < count; i++) {
            files[i] = [[filenames objectAtIndex:i] UTF8String];
        }
        self.callback(count, files);
    }
}
@end

// Global instance
static MacOpenFileDelegate* delegate = nil;

void RegisterOpenFilesCallback(OpenFilesCallback cb) {
    delegate = [MacOpenFileDelegate new];
    delegate.callback = cb;
    [NSApp setDelegate:delegate];
}
