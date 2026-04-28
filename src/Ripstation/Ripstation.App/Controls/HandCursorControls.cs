using Microsoft.UI.Input;
using Microsoft.UI.Xaml.Controls;

namespace Ripstation.Controls;

/// <summary>Button that shows a hand/pointer cursor on hover.</summary>
public class HandCursorButton : Button
{
    public HandCursorButton() =>
        ProtectedCursor = InputSystemCursor.Create(InputSystemCursorShape.Hand);
}

/// <summary>CheckBox that shows a hand/pointer cursor on hover.</summary>
public class HandCursorCheckBox : CheckBox
{
    public HandCursorCheckBox() =>
        ProtectedCursor = InputSystemCursor.Create(InputSystemCursorShape.Hand);
}

/// <summary>RadioButton that shows a hand/pointer cursor on hover.</summary>
public class HandCursorRadioButton : RadioButton
{
    public HandCursorRadioButton() =>
        ProtectedCursor = InputSystemCursor.Create(InputSystemCursorShape.Hand);
}
