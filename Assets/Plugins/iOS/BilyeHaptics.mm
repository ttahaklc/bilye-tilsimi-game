#import <UIKit/UIKit.h>

// kind: 0 light, 1 medium, 2 heavy, 3 selection, 4 success, 5 error
extern "C" void _BilyeHaptic(int kind)
{
    if (@available(iOS 10.0, *))
    {
        if (kind == 3)
        {
            UISelectionFeedbackGenerator *g = [[UISelectionFeedbackGenerator alloc] init];
            [g selectionChanged];
        }
        else if (kind == 4 || kind == 5)
        {
            UINotificationFeedbackGenerator *g = [[UINotificationFeedbackGenerator alloc] init];
            [g notificationOccurred:(kind == 4 ? UINotificationFeedbackTypeSuccess : UINotificationFeedbackTypeError)];
        }
        else
        {
            UIImpactFeedbackStyle s = kind == 0 ? UIImpactFeedbackStyleLight : (kind == 1 ? UIImpactFeedbackStyleMedium : UIImpactFeedbackStyleHeavy);
            UIImpactFeedbackGenerator *g = [[UIImpactFeedbackGenerator alloc] initWithStyle:s];
            [g impactOccurred];
        }
    }
}
