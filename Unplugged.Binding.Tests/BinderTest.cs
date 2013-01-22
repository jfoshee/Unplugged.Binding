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
        
        [Test]
        public void InitializeLabel()
        {
            var expected = "Expected text";
            var viewModel = new SampleStaticViewModel { RockDoveText = expected };
            var view = new SampleView();
            
            Subject.Bind(viewModel, view);
            
            Assert.That(view.RockDove.Text, Is.EqualTo(expected));
            Assert.That(view.SomeOther.Text, Is.EqualTo(default(string)));
        }
        
        [Test]
        public void UpdateLabelText()
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
        /// 3. Unhook in dispose
        /// 4. Ignore properties w/out matching name

        public Binder Subject { get; set; }
        [SetUp]
        public void Setup()
        {
            Subject = new Binder();
        }
    }
}

