# HAtxLib - Android UI Automation Library

![.NET Framework 4.7.2](https://img.shields.io/badge/.NET%20Framework-4.7.2-blue)
![License](https://img.shields.io/badge/license-MIT-green)

HAtxLib is a powerful C# library for Android UI automation built on top of [uiautomator2](https://github.com/openatx/uiautomator2). It provides a clean and intuitive API for controlling Android devices, automating UI interactions, and managing applications.

## ✨ Features

- 🎯 **Screen Operations** - Click, swipe, drag, and touch gestures
- 📱 **Device Management** - Screen orientation, power control, device info
- 📦 **App Management** - Start, stop, install, uninstall applications
- ⌨️ **Input Method Control** - Text input and IME management
- 🔍 **UI Element Finding** - Powerful selector-based element location
- 🔄 **Async Support** - Async/await versions of key methods
- 🛡️ **Robust Error Handling** - Retry logic and comprehensive logging
- 🏗️ **Modular Architecture** - Clean separation of concerns

## 📦 Installation

### Via NuGet (Coming Soon)
```bash
Install-Package HAtxLib
```

### Manual Installation
1. Clone this repository
2. Build the solution
3. Reference `HAtxLib.dll` in your project

## 🚀 Quick Start

### Basic Device Connection

```csharp
using HAtxLib;

// Connect to device (auto-initialization)
var device = new HAtx("device_serial");

// Or connect without auto-initialization
var device = new HAtx("device_serial", init: false);
```

### Screen Operations

```csharp
// Click at coordinates (can use absolute or relative)
device.Click(100, 200);           // Absolute coordinates
device.Click(0.5f, 0.5f);         // Relative (center of screen)

// Double click
device.DoubleClick(0.5f, 0.5f);

// Long press
device.LongClick(0.5f, 0.5f, duration: 1000);

// Swipe
device.Swipe(0.5f, 0.8f, 0.5f, 0.2f); // Swipe up from bottom to top

// Drag
device.Drag(0.2f, 0.5f, 0.8f, 0.5f);  // Drag from left to right

// Advanced touch control
device.TouchDown(0.5f, 0.5f);
device.TouchMove(0.6f, 0.6f);
device.TouchUp(0.6f, 0.6f);
```

### Device Information

```csharp
// Get device info
var info = device.Device.Info();
Console.WriteLine($"Brand: {info.Brand}");
Console.WriteLine($"Model: {info.Model}");
Console.WriteLine($"Battery: {info.Battery.Level}%");

// Check if device is alive
bool isAlive = device.IsAlive();

// Get screen orientation
var orientation = device.GetOrientation();

// Set screen orientation
device.SetOrientation(DeviceManager.Orientation.Natural);

// Screen power control
device.ScreenOn();
device.ScreenOff();

// Get screen size
var size = device.GetWindowSize();
Console.WriteLine($"Screen: {size.Width}x{size.Height}");
```

### App Management

```csharp
// Start an app
device.AppStart("com.example.app");

// Start with specific activity
device.AppStart("com.example.app", activity: ".MainActivity");

// Wait for app to start
bool started = device.AppWait("com.example.app", timeout: 5000);

// Get current app
var currentApp = device.AppCurrent();
Console.WriteLine($"Current: {currentApp.Package}");

// Stop an app
device.AppStop("com.example.app");

// Clear app data
device.AppClear("com.example.app");

// Uninstall
device.AppUninstall("com.example.app");

// Stop all apps except system
device.AppStopAll("com.android.systemui", "com.android.settings");
```

### Input Method (IME)

```csharp
// Set fast input IME
device.ImeSet(true);

// Wait for IME to be ready
device.ImeWait();

// Input text
device.ImeInputText("Hello World");

// Input text with clear
device.ImeInputText("New Text", clear: true);

// Clear text
device.ImeClearText();

// Check current IME
bool shown = device.ImeCurrent(out string ime);
Console.WriteLine($"IME: {ime}, Shown: {shown}");
```

### UI Element Finding

```csharp
// Find element by text
var element = device.FindElement(By.Text("Login"));
element.Click();

// Find by resource ID
var button = device.FindElement(By.ResourceId("com.example:id/button"));

// Find by description
var image = device.FindElement(By.Description("Profile Image"));

// Check if element exists
if (element.Exists())
{
    element.Click();
}

// Wait for element
element.WaitFor(timeout: 5000);

// Get element info
var bounds = element.Bounds;
var text = element.Text;
```

### Press Keys

```csharp
// Press home button
device.Press(PressKey.Home);

// Press back
device.Press(PressKey.Back);

// Press menu
device.Press(PressKey.Menu);

// Other available keys: Enter, Delete, Recent, VolumeUp, VolumeDown, etc.
```

### Async Operations

```csharp
// Use async versions for better performance
await device.ClickAsync(0.5f, 0.5f);
await device.SwipeAsync(0.5f, 0.8f, 0.5f, 0.2f);

string hierarchy = await device.DumpHierarchyAsync();
bool alive = await device.IsAliveAsync();

await device.AppStartAsync("com.example.app");
bool started = await device.AppWaitAsync("com.example.app");
```

## 🏗️ Architecture

HAtxLib follows a modular architecture with specialized managers:

- **ScreenOperations** - Handles all screen interactions (click, swipe, drag)
- **AppManager** - Manages application lifecycle and information
- **InputMethodManager** - Controls input methods and text input
- **DeviceManager** - Provides device information and control
- **RetryHelper** - Implements retry logic for robust operations
- **AtxConstants** - Centralized configuration and constants

## 🔧 Configuration

You can customize various settings:

```csharp
// Global settings
device.UINodeMaxWaitTime = 5000;           // Max wait time for UI operations
device.UINodeClickExistDelay = 100;        // Delay before click
device.UINodeClickDelay = 150;             // Delay after click

// Debug mode
device.SetDebug(true);
```

## 📝 Examples

### Complete Login Flow

```csharp
var device = new HAtx("emulator-5554");

// Start the app
device.AppStart("com.example.app");
device.AppWait("com.example.app", timeout: 10000);

// Find and fill username
var username = device.FindElement(By.ResourceId("username"));
username.Click();
device.ImeInputText("user@example.com");

// Find and fill password
var password = device.FindElement(By.ResourceId("password"));
password.Click();
device.ImeInputText("password123", clear: true);

// Click login button
var loginBtn = device.FindElement(By.Text("Login"));
loginBtn.Click();

// Wait for home screen
device.WaitActivity(".HomeActivity", timeout: 5000);
```

### Taking Screenshots

```csharp
// Dump UI hierarchy
string xml = device.DumpHierarchy();
File.WriteAllText("hierarchy.xml", xml);

// Or compressed version
string compressed = device.DumpWindowHierarchy(compressed: true);
```

## 🔍 Troubleshooting

### Device Not Found
Ensure ADB is running and device is connected:
```bash
adb devices
```

### UIAutomator Not Starting
Try manually installing UIAutomator:
```csharp
device.Initer.Install();
```

### Connection Issues
Check if device is alive:
```csharp
if (!device.IsAlive())
{
    // Reinitialize connection
    device = new HAtx("device_serial");
}
```

## 🤝 Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

## 📄 License

This project is licensed under the MIT License - see the LICENSE file for details.

## 🙏 Acknowledgments

- Built on top of [openatx/uiautomator2](https://github.com/openatx/uiautomator2)
- Inspired by the Python uiautomator2 library

## 📞 Support

For issues and questions:
- Open an issue on GitHub
- Check existing documentation
- Review example code

## 🔗 Related Projects

- [uiautomator2](https://github.com/openatx/uiautomator2) - Python automation library
- [atx-agent](https://github.com/openatx/atx-agent) - Android agent for automation

---

Made with ❤️ by the HAtxLib team
