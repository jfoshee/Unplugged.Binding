using System;
using NUnit.Framework;

namespace Unplugged.Binding.Tests
{
    [TestFixture]
    public class BinderTest
    {
        class SampleStaticViewModel
        {
            public string RockDoveText { get; set; }
        }

        class SampleViewModel : ViewModelBase
        {
            string _pumaText;
            public string PumaText
            {
                get { return _pumaText; }
                set { _pumaText = value; Notify("PumaText"); }
            }
        }

        class Label
        {
            public string Text { get; set; }
        }

        class SampleView
        {
            public Label RockDove { get; set; }
            public Label Puma { get; set; }
            public Label SomeOther { get; set; }
            
            public SampleView()
            {
                RockDove = new Label();
                Puma = new Label();
                SomeOther = new Label();
            }
        }

        class SampleViewWithSuffix 
        {
            public Label RockDoveLabel { get; set; }
            public Label PumaLabel { get; set; }
            public Label SomeOtherLabel { get; set; }
            
            public SampleViewWithSuffix()
            {
                RockDoveLabel = new Label();
                PumaLabel = new Label();
                SomeOtherLabel = new Label();
            }
        }

        class BasicObject
        {
            public string BasicString { get; set; }
            public int BasicNumber { get; set; }
            public object BasicReference { get; set; }
        }

        class BasicViewModel : ViewModelBase
        {
            string _basicString;
            public string BasicString
            {
                get { return _basicString; }
                set { _basicString = value; Notify("BasicString"); }
            }
        }

        [Test]
        public void InitializeSameNames()
        {
            var expected = "Some expected text";
            var viewModel = new { BasicString = expected, UnusedInVM = "whatever" };
            var view = new BasicObject();

            Subject.Bind(viewModel, view);

            Assert.That(view.BasicString, Is.EqualTo(expected));
            Assert.That(view.BasicReference, Is.Null);
        }

        [Test]
        public void UpdateSameNames()
        {
            var expected = "Some changed text";
            var viewModel = new BasicViewModel();
            var view = new BasicObject();
            Subject.Bind(viewModel, view);

            viewModel.BasicString = expected;
            
            Assert.That(view.BasicString, Is.EqualTo(expected));
        }
        
        [Test]
        public void InitializeControlsTextProperty()
        {
            var expected = "Expected text";
            var viewModel = new SampleStaticViewModel { RockDoveText = expected };
            var view = new SampleView();
            
            Subject.Bind(viewModel, view);
            
            Assert.That(view.RockDove.Text, Is.EqualTo(expected));
            Assert.That(view.SomeOther.Text, Is.EqualTo(default(string)));
        }

        [Test]
        public void UpdateControlsTextProperty()
        {
            var expected = "Changed";
            var viewModel = new SampleViewModel { PumaText = "Initial value" };
            var view = new SampleView();
            Subject.Bind(viewModel, view);
            
            viewModel.PumaText = expected;
            
            Assert.That(view.Puma.Text, Is.EqualTo(expected));
        }

        /// Requirements:
        /// 1. Initialize
        /// 2. Update on NotifyPropertyChanged
        ///   a. Immediately
        ///   b. On a specific thread
        /// 3. Unhook in dispose
        /// 4. Ignore properties w/out matching name
        /// 
        /// Naming Conventions
        /// - Foo : Foo
        /// - FooText : Foo.Text
        /// - FooText : FooLabel.Text
        /// 
        /// Log: Control does not have property of given name (e.g. Text)
        /// Log: Mismatched types
        /// 

        public Binder Subject { get; set; }
        [SetUp]
        public void Setup()
        {
            Subject = new Binder();
        }
    }
}

