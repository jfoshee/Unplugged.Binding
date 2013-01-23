using System;
using NUnit.Framework;

namespace Unplugged.Binding.Tests
{
    [TestFixture]
    public class BindingFactoryTest
    {
        #region Samples

        class SampleStaticViewModel
        {
            public string RockDoveText { get; set; }
        }

        class SampleViewModel : ViewModelBase
        {
            string _puma;
            public string Puma
            {
                get { return _puma; }
                set { _puma = value; Notify("Puma"); }
            }

            string _private;
            public string Private
            {
                get { return _private; }
                set { _private = value; Notify("Private"); }
            }
        }

        class SampleViewModelWithSuffix : ViewModelBase
        {
            string _pumaText;
            public string PumaText
            {
                get { return _pumaText; }
                set { _pumaText = value; Notify("PumaText"); }
            }

            string _privateText;
            public string PrivateText
            {
                get { return _privateText; }
                set { _privateText = value; Notify("PrivateText"); }
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
            Label PrivateLabel { get; set; }
            
            public string PrivateLabelText()
            {
                return PrivateLabel.Text;
            }

            public SampleViewWithSuffix()
            {
                RockDoveLabel = new Label();
                PumaLabel = new Label();
                SomeOtherLabel = new Label();
                PrivateLabel = new Label();
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

            object _basicReference;
            public object BasicReference
            {
                get { return _basicReference; }
                set { _basicReference = value; Notify("BasicReference"); }
            }
        }

        class BasicViewWithPrivateSetter
        {
            public string BasicString { get; private set; }
            public object BasicReference { get { return "Hello"; } }
        }

        #endregion

        [Test]
        public void InitializeSameNames()
        {
            const string expected = "Some expected text";
            var viewModel = new { BasicString = expected, UnusedInVM = "whatever" };
            var view = new BasicObject();

            Subject.Bind(viewModel, view);

            Assert.That(view.BasicString, Is.EqualTo(expected));
            Assert.That(view.BasicReference, Is.Null);
        }

        [Test]
        public void UpdateSameNames()
        {
            const string expected = "Some changed text";
            var viewModel = new BasicViewModel();
            var view = new BasicObject();
            Subject.Bind(viewModel, view);

            viewModel.BasicString = expected;
            
            Assert.That(view.BasicString, Is.EqualTo(expected));
        }
        
        [Test]
        public void InitializeControlsTextProperty()
        {
            const string expected = "Expected text";
            var viewModel = new SampleStaticViewModel { RockDoveText = expected };
            var view = new SampleView();
            
            Subject.Bind(viewModel, view);
            
            Assert.That(view.RockDove.Text, Is.EqualTo(expected));
            Assert.That(view.SomeOther.Text, Is.EqualTo(default(string)));
        }

        [Test]
        public void UpdateControlsTextProperty()
        {
            const string expected = "Changed";
            var viewModel = new SampleViewModelWithSuffix { PumaText = "Initial value" };
            var view = new SampleView();
            Subject.Bind(viewModel, view);
            
            viewModel.PumaText = expected;
            
            Assert.That(view.Puma.Text, Is.EqualTo(expected));
        }

        [Test]
        public void InitializeWithControlSuffix()
        {
            const string expected = "Expected text";
            var viewModel = new SampleStaticViewModel { RockDoveText = expected };
            var view = new SampleViewWithSuffix();

            Subject.Bind(viewModel, view);

            Assert.That(view.RockDoveLabel.Text, Is.EqualTo(expected));
            Assert.That(view.SomeOtherLabel.Text, Is.EqualTo(default(string)));
        }

        [Test]
        public void UpdateControlsTextPropertyWithSuffix()
        {
            const string expected = "Changed";
            var viewModel = new SampleViewModelWithSuffix { PumaText = "Initial value" };
            var view = new SampleViewWithSuffix();
            Subject.Bind(viewModel, view);

            viewModel.PumaText = expected;

            Assert.That(view.PumaLabel.Text, Is.EqualTo(expected));
        }

        [Test]
        public void UnhookOnDispose()
        {
            const string expected = "Initial value";
            var viewModel = new SampleViewModelWithSuffix { PumaText = expected };
            var view = new SampleView();
            var binding = Subject.Bind(viewModel, view);

            binding.Dispose();

            viewModel.PumaText = "changed";
            Assert.That(view.Puma.Text, Is.EqualTo(expected));
        }

        [Test]
        public void InvokeWithGivenInvoker()
        {
            Action savedForLater = null;
            Action<Action> updateViewInvoker = (a) => { savedForLater = a; };
            Subject.UpdateView = updateViewInvoker;
            const string expected = "Some changed text";
            var viewModel = new BasicViewModel();
            var view = new BasicObject();
            Subject.Bind(viewModel, view);
            viewModel.BasicString = expected;
            Assert.That(view.BasicString, Is.EqualTo(default(string)));

            savedForLater();
            
            Assert.That(view.BasicString, Is.EqualTo(expected));
        }

        [Test]
        public void IgnorePropertiesWithPrivateSetter()
        {
            var viewModel = new BasicViewModel { BasicString = "foo" };
            var view = new BasicViewWithPrivateSetter();

            Subject.Bind(viewModel, view);

            Assert.That(view.BasicString, Is.EqualTo(default(string)));
        }

        [Test]
        public void IgnorePropertiesWithoutSetter()
        {
            const string expected = "Exp";
            var viewModel = new BasicViewModel { BasicString = "foo" };
            var view = new { BasicString = expected };

            Subject.Bind(viewModel, view);

            Assert.That(view.BasicString, Is.EqualTo(expected));
        }

        [Test]
        public void HasPriorValue()
        {
            var expected = new SampleView();
            var viewModel = new BasicViewModel { BasicReference = expected };
            var view = new BasicObject { BasicReference = this};

            Subject.Bind(viewModel, view);

            Assert.That(view.BasicReference, Is.SameAs((expected)));
        }

        [Test]
        public void PropertyChangedWithoutPropertyInView()
        {
            var viewModel = new BasicViewModel();
            var view = new SampleView();
            Subject.Bind(viewModel, view);
            viewModel.BasicReference = this;
        }

        [Test]
        public void MustFindPrivateProperties()
        {
            const string expected = "We have to do this because MonoTouch Outlets are non-public";
            var viewModel = new SampleViewModelWithSuffix();
            var view = new SampleViewWithSuffix();
            Subject.Bind(viewModel, view);

            viewModel.PrivateText = expected;

            Assert.That(view.PrivateLabelText(), Is.EqualTo(expected));
        }

        [Test]
        public void MatchViewWithSuffixAndControlProperty()
        {
            const string expected = "changed";
            var viewModel = new SampleViewModel { Puma = "initial" };
            var view = new SampleViewWithSuffix();
            Subject.Bind(viewModel, view);
            Assert.That(view.PumaLabel.Text, Is.EqualTo("initial"));

            viewModel.Puma = expected;

            Assert.That(view.PumaLabel.Text, Is.EqualTo(expected));
        }

        [Test]
        public void ConvertToStringIfDestinationIsStringButSourceIsNot()
        {
            var viewModel = new {Puma = 12};
            var view = new SampleViewWithSuffix();

            Subject.Bind(viewModel, view);

            Assert.That(view.PumaLabel.Text, Is.EqualTo("12"));
        }

        /// To do: 
        /// - 

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
        /// - Foo : FooLabel.Text
        /// - Foo : Foo.Text
        /// 
        /// Nice to Have:
        /// - Log: Control does not have property of given name (e.g. Text)
        /// - Log: Mismatched types
        /// - Cache reflection

        public BindingFactory Subject { get; set; }
        [SetUp]
        public void Setup()
        {
            Subject = new BindingFactory();
        }
    }
}
