// SPDX-FileCopyrightText: 2026 Froststrap
//
// SPDX-License-Identifier: MPL-2.0

using Avalonia.Animation;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Media.Imaging;
using Froststrap.UI.Elements.Bootstrapper.Base;
using Froststrap.UI.ViewModels.Bootstrapper;

namespace Froststrap.UI.Elements.Bootstrapper
{
    internal partial class FluentDialog : AvaloniaDialogBase
    {
        private readonly FluentDialogViewModel? _viewModel;

        public FluentDialog()
        {
            InitializeComponent();
        }

        public FluentDialog(bool aero) : this()
        {
            string version = Froststrap.Utility.Versioning.GetRobloxVersionStr(Bootstrapper?.IsStudioLaunch ?? false);
            _viewModel = new FluentDialogViewModel(this, aero, version);
            DataContext = _viewModel;

            SetupDialog();

            var iconImage = App.Settings.Prop.BootstrapperIcon.GetIcon().GetImageSource();
            if (iconImage is Bitmap bitmap)
                Icon = new WindowIcon(bitmap);

            if (OperatingSystem.IsLinux())
                WindowDecorations = WindowDecorations.None; // niri draws no chrome; Mod+Q or Skip closes

            // the card floats, so it can be moved from anywhere that is not the Skip button
            PointerPressed += (_, e) =>
            {
                if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed && !e.Handled)
                    BeginMoveDrag(e);
            };
        }

        /// <summary>Appear: fade in with a 6px upward drift, 240 ms ease-out (matches niri's open).</summary>
        protected override void OnOpened(EventArgs e)
        {
            base.OnOpened(e);

            if (App.Settings.Prop.DisableAnimations || this.FindControl<Border>("Card") is not { } card)
                return;

            card.RenderTransform = new TranslateTransform();
            var appear = new Animation
            {
                Duration = TimeSpan.FromMilliseconds(240),
                Easing = new CubicEaseOut(),
                FillMode = FillMode.Forward,
                Children =
                {
                    new KeyFrame { Cue = new Cue(0d), Setters = { new Setter(OpacityProperty, 0d), new Setter(TranslateTransform.YProperty, 6d) } },
                    new KeyFrame { Cue = new Cue(1d), Setters = { new Setter(OpacityProperty, 1d), new Setter(TranslateTransform.YProperty, 0d) } }
                }
            };
            _ = appear.RunAsync(card);
        }

        #region UI Elements Overrides
        public override string Message
        {
            get => _viewModel!.Message;
            set => RunOnUI(() =>
            {
                _viewModel!.Message = value;
                _viewModel.OnPropertyChanged(nameof(_viewModel.Message));
            });
        }

        public override int ProgressMaximum
        {
            get => _viewModel!.ProgressMaximum;
            set => RunOnUI(() =>
            {
                _viewModel!.ProgressMaximum = value;
                _viewModel.OnPropertyChanged(nameof(_viewModel.ProgressMaximum));
            });
        }

        public override int ProgressValue
        {
            get => _viewModel!.ProgressValue;
            set => RunOnUI(() =>
            {
                _viewModel!.ProgressValue = value;
                _viewModel.OnPropertyChanged(nameof(_viewModel.ProgressValue));
            });
        }

        public override bool CancelEnabled
        {
            get => _viewModel!.CancelEnabled;
            set => RunOnUI(() =>
            {
                _viewModel!.CancelEnabled = value;
                _viewModel.OnPropertyChanged(nameof(_viewModel.CancelEnabled));
                _viewModel.OnPropertyChanged(nameof(_viewModel.CancelButtonVisible));
            });
        }

        public override bool ProgressIndeterminate
        {
            get => _viewModel!.ProgressIndeterminate;
            set => RunOnUI(() =>
            {
                _viewModel!.ProgressIndeterminate = value;
                _viewModel.OnPropertyChanged(nameof(_viewModel.ProgressIndeterminate));
            });
        }
        #endregion
    }
}
