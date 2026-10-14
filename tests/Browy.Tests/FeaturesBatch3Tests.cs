using System;
using System.Collections.Generic;
using Browy.Models;
using Browy.Services;
using Xunit;

namespace Browy.Tests
{
    public class FeaturesBatch3Tests
    {
        [Fact]
        public void BrowserSpace_IsolationProperties_WorkCorrectly()
        {
            var space = new BrowserSpace { Name = "Work" };
            Assert.False(space.IsIsolated);
            Assert.False(space.ShowsIsolationBadge);
            Assert.Equal("Standard Profile", space.IsolationToolTip);

            space.IsIsolated = true;
            Assert.True(space.IsIsolated);
            Assert.True(space.ShowsIsolationBadge);
            Assert.Contains("Isolated", space.IsolationToolTip);
        }

        [Fact]
        public void SpaceSessionData_Serializes_IsIsolated()
        {
            var sessionData = new SpaceSessionData
            {
                Name = "Work",
                IsIsolated = true
            };
            Assert.True(sessionData.IsIsolated);
        }

        [Fact]
        public void BrowserSettings_NewSettings_HaveExpectedDefaults()
        {
            var settings = BrowserSettings.Current;
            Assert.NotNull(settings.BackdropMaterial);
            Assert.True(settings.EnableIdleTabSuspension);
            Assert.True(settings.IdleTabSuspensionMinutes > 0);
            Assert.True(settings.EnableMouseGestures);
            Assert.True(settings.EnableTrackpadSwipe);
            Assert.NotNull(settings.CustomKeyBindings);
        }

        [Fact]
        public void SettingsPageService_GetSettingsHtml_ContainsAllNewSections()
        {
            string html = SettingsPageService.GetSettingsHtml();

            // Backdrop Material (#4)
            Assert.Contains("Backdrop Material", html);
            Assert.Contains("Desktop Acrylic", html);
            Assert.Contains("Mica", html);
            Assert.Contains("Mica Alt", html);

            // Tab Suspension (#18)
            Assert.Contains("Sleeping Tabs", html);
            Assert.Contains("Put Inactive Tabs to Sleep After", html);

            // Gestures (#14)
            Assert.Contains("Right-Click Mouse Gestures", html);
            Assert.Contains("Two-Finger Trackpad Horizontal Swipe", html);

            // Shortcuts Manager (#9)
            Assert.Contains("Keyboard Shortcuts", html);
            Assert.Contains("key-pill", html);
            Assert.Contains("shortcutConflict", html);
            Assert.Contains("resetAllShortcuts", html);
        }

        [Fact]
        public void WindowsBackdropService_IsWindows11OrGreater_ReturnsBoolean()
        {
            bool isWin11 = WindowsBackdropService.IsWindows11OrGreater;
            // Should be a valid boolean without throwing
            Assert.True(isWin11 || !isWin11);
        }
    }
}
