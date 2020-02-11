﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright file="InterfaceScaler.cs" company="Chris Dziemborowicz">
//   Copyright (c) Chris Dziemborowicz. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Hourglass.Windows
{
    using System;
    using System.Windows;
    using System.Windows.Controls;

    using Hourglass.Extensions;
    using Hourglass.Timing;

    /// <summary>
    /// Scales a <see cref="TimerWindow"/> to ensure that controls shrink and grow with the window size, and updates a
    /// timer interval to ensure smooth animation of the progress bar.
    /// </summary>
    public class InterfaceScaler
    {
        /// <summary>
        /// The default margin for <see cref="Button"/> elements in the <see cref="timerWindow"/>.
        /// </summary>
        private const double BaseButtonMargin = 7;

        /// <summary>
        /// The default margin for the <see cref="Grid"/> control containing the controls of the <see
        /// cref="timerWindow"/> when the current environment and platform is Windows 8.1 or older.
        /// </summary>
        private const double BaseControlsGridMarginForWindows81AndOlder = 10;

        /// <summary>
        /// The default margin for the <see cref="Grid"/> control containing the controls of the <see
        /// cref="timerWindow"/> when the current environment and platform is Windows 10 or newer.
        /// </summary>
        private const double BaseControlsGridMarginForWindows10AndNewer = 13;

        /// <summary>
        /// The default margin for the <see cref="StackPanel"/> control containing the controls of the <see
        /// cref="timerWindow"/>.
        /// </summary>
        private const double BaseControlsPanelMargin = 20;

        /// <summary>
        /// The default font size used to render controls in the <see cref="timerWindow"/>.
        /// </summary>
        private const double BaseFontSize = 12;

        /// <summary>
        /// The default font size of the larger text control on the <see cref="timerWindow"/>.
        /// </summary>
        private const double BasePrimaryTextControlFontSize = 18;

        /// <summary>
        /// The default additional margin of the larger text control on the <see cref="timerWindow"/>, which is used
        /// when the base scale factor exceeds 1.0.
        /// </summary>
        private const double BasePrimaryTextControlAdditionalMargin = 10;

        /// <summary>
        /// The default top margin of the larger text control on the <see cref="timerWindow"/>.
        /// </summary>
        private const double BasePrimaryTextControlTopMargin = 1;

        /// <summary>
        /// The default bottom margin of the larger text control on the <see cref="timerWindow"/>.
        /// </summary>
        private const double BasePrimaryTextControlBottomMargin = 2;

        /// <summary>
        /// The default border thickness for the <see cref="Border"/> control that visualize validation errors and
        /// expired timer state.
        /// </summary>
        private const double BaseBorderThickness = 1;

        /// <summary>
        /// The default margin for the <see cref="Border"/> control that visualize validation errors and expired timer
        /// state.
        /// </summary>
        private const double BaseBorderMargin = 15;

        /// <summary>
        /// The default width of a <see cref="timerWindow"/>.
        /// </summary>
        private const double BaseWindowWidth = 350;

        /// <summary>
        /// The default height of a <see cref="timerWindow"/>.
        /// </summary>
        private const double BaseWindowHeight = 150;

        /// <summary>
        /// The reduction factor that relates the base scale factor with the reduced scale factor.
        /// </summary>
        private const double ReductionFactor = 0.5;

        /// <summary>
        /// A <see cref="TimerWindow"/>.
        /// </summary>
        private TimerWindow timerWindow;

        /// <summary>
        /// The <see cref="Grid"/> control that contains the <see cref="controlsPanel"/>.
        /// </summary>
        private Grid innerGrid;

        /// <summary>
        /// The <see cref="StackPanel"/> control that contains the controls of the <see cref="timerWindow"/>.
        /// </summary>
        private StackPanel controlsPanel;

        /// <summary>
        /// The larger <see cref="TextBox"/> on the <see cref="timerWindow"/>.
        /// </summary>
        private SizeToFitTextBox timerTextBox;

        /// <summary>
        /// The smaller <see cref="TextBox"/> on the <see cref="timerWindow"/>.
        /// </summary>
        private SizeToFitTextBox titleTextBox;

        /// <summary>
        /// The <see cref="Border"/> that animates to notify the user that the timer has expired or that the input was
        /// invalid.
        /// </summary>
        private Border innerNotificationBorder;

        /// <summary>
        /// An array of the <see cref="Button"/> elements on the <see cref="timerWindow"/>.
        /// </summary>
        private Button[] buttons;

        /// <summary>
        /// The <see cref="Label"/> that contains the time elapsed since the timer expired when the timer has expired.
        /// </summary>
        private Label timeExpiredLabel;

        /// <summary>
        /// Binds the <see cref="InterfaceScaler"/> to a <see cref="TimerWindow"/>.
        /// </summary>
        /// <param name="window">A <see cref="TimerWindow"/>.</param>
        public void Bind(TimerWindow window)
        {
            // Validate parameters
            if (window == null)
            {
                throw new ArgumentNullException("window");
            }

            // Validate state
            if (this.timerWindow != null)
            {
                throw new InvalidOperationException();
            }

            // Initialize members
            this.timerWindow = window;

            this.innerGrid = this.timerWindow.InnerGrid;
            this.controlsPanel = this.timerWindow.ControlsPanel;
            this.timerTextBox = this.timerWindow.TimerTextBox;
            this.titleTextBox = this.timerWindow.TitleTextBox;
            this.innerNotificationBorder = this.timerWindow.InnerNotificationBorder;
            this.buttons = new[]
            {
                this.timerWindow.StartButton,
                this.timerWindow.PauseButton,
                this.timerWindow.ResumeButton,
                this.timerWindow.StopButton,
                this.timerWindow.ResetButton,
                this.timerWindow.CloseButton,
                this.timerWindow.CancelButton,
                this.timerWindow.SaveButton
            };
            this.timeExpiredLabel = this.timerWindow.TimeExpiredLabel;

            // Hook up events
            this.timerWindow.Loaded += (s, e) => this.Scale();
            this.timerWindow.SizeChanged += (s, e) => this.Scale();
            this.timerWindow.PropertyChanged += (s, e) => this.Scale();
            this.timerTextBox.TextChanged += (s, e) => this.Scale();
        }

        /// <summary>
        /// Scales the <see cref="TimerWindow"/> to ensure that controls shrink and grow with the window size, and
        /// updates the timer interval to ensure smooth animation of the progress bar.
        /// </summary>
        public void Scale()
        {
            this.ScaleControls();
            this.ScaleTimerInterval();
        }

        /// <summary>
        /// Scales the <see cref="timerWindow"/> and its controls.
        /// </summary>
        private void ScaleControls()
        {
            if (!this.timerWindow.IsVisible)
            {
                return;
            }

            double baseScaleFactor = this.GetBaseScaleFactor();
            double reducedScaleFactor = this.GetReducedScaleFactor(baseScaleFactor, ReductionFactor);
            this.ScaleControls(baseScaleFactor, reducedScaleFactor);
        }

        /// <summary>
        /// Scales the <see cref="timerWindow"/> and its controls using the specified factors.
        /// </summary>
        /// <param name="baseScaleFactor">The base scale factor.</param>
        /// <param name="reducedScaleFactor">The reduced scale factor.</param>
        /// <seealso cref="GetBaseScaleFactor"/>
        /// <seealso cref="GetReducedScaleFactor"/>
        private void ScaleControls(double baseScaleFactor, double reducedScaleFactor)
        {
            double baseControlsGridMargin = EnvironmentExtensions.IsWindows10OrNewer
                ? BaseControlsGridMarginForWindows10AndNewer
                : BaseControlsGridMarginForWindows81AndOlder;
            this.innerGrid.Margin = new Thickness(reducedScaleFactor * baseControlsGridMargin);

            this.controlsPanel.Margin = new Thickness(
                left: reducedScaleFactor * BaseControlsPanelMargin,
                top: 0.0,
                right: reducedScaleFactor * BaseControlsPanelMargin,
                bottom: 0.0);

            this.timerTextBox.MaxFontSize = baseScaleFactor * BasePrimaryTextControlFontSize;
            this.timerTextBox.Margin = new Thickness(
                left: 0.0,
                top: (baseScaleFactor * BasePrimaryTextControlTopMargin) + ((baseScaleFactor - 1.0) * BasePrimaryTextControlAdditionalMargin),
                right: 0.0,
                bottom: (baseScaleFactor * BasePrimaryTextControlBottomMargin) + ((baseScaleFactor - 1.0) * BasePrimaryTextControlAdditionalMargin));

            this.titleTextBox.MaxFontSize = reducedScaleFactor * BaseFontSize;

            this.innerNotificationBorder.BorderThickness = new Thickness(reducedScaleFactor * BaseBorderThickness);
            this.innerNotificationBorder.Margin = new Thickness(reducedScaleFactor * BaseBorderMargin);

            foreach (Button button in this.buttons)
            {
                button.FontSize = reducedScaleFactor * BaseFontSize;
                button.Margin = new Thickness(
                    left: baseScaleFactor * BaseButtonMargin,
                    top: 0.0,
                    right: baseScaleFactor * BaseButtonMargin,
                    bottom: 0.0);
            }

            this.timeExpiredLabel.FontSize = reducedScaleFactor * BaseFontSize;
        }

        /// <summary>
        /// Returns the base scale factor for the user interface based on the width and height of the <see
        /// cref="timerWindow"/>.
        /// </summary>
        /// <returns>The base scale factor.</returns>
        private double GetBaseScaleFactor()
        {
            double widthFactor = Math.Max(this.timerWindow.ActualWidth / BaseWindowWidth, 1.0);
            double heightFactor = Math.Max(this.timerWindow.ActualHeight / BaseWindowHeight, 1.0);
            return Math.Min(widthFactor, heightFactor);
        }

        /// <summary>
        /// Returns the reduced scale factor. The reduced scale factor is computed by reducing the portion of the scale
        /// factor that exceeds 1.0 by the reduction factor. If the base scale factor is less than or equal to 1.0, the
        /// reduced scale factor is 1.0.
        /// </summary>
        /// <param name="baseScaleFactor">The base scale factor.</param>
        /// <param name="reductionFactor">The reduction factor.</param>
        /// <returns>The reduced scale factor.</returns>
        private double GetReducedScaleFactor(double baseScaleFactor, double reductionFactor)
        {
            double difference = baseScaleFactor - 1.0;
            return 1.0 + (difference * reductionFactor);
        }

        /// <summary>
        /// Updates the timer interval to ensure smooth animation of the progress bar.
        /// </summary>
        private void ScaleTimerInterval()
        {
            if (!this.timerWindow.IsVisible)
            {
                return;
            }

            Timer timer = this.timerWindow.Timer;

            if (timer.TotalTime.HasValue)
            {
                double interval = timer.TotalTime.Value.TotalMilliseconds / this.timerWindow.ActualWidth / 2.0;
                interval = MathExtensions.LimitToRange(interval, 10.0, TimerBase.DefaultInterval.TotalMilliseconds);
                timer.Interval = TimeSpan.FromMilliseconds(interval);
            }
            else
            {
                timer.Interval = TimerBase.DefaultInterval;
            }
        }
    }
}
