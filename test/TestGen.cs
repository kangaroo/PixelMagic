using System;
using System.Windows.Media.Imaging;
using System.Windows.Media.Effects;
using System.Windows.Controls;
using System.Windows.Media;
using System.IO;
using System.Windows;
using System.Globalization;
using System.Windows.Shapes;

namespace ShaderTestGen
{	
	public class TestSuite {
		public static readonly TestCase[] Tests = new TestCase[] {
			new TestCase ("invert-color") {
				Effect = "shaders/InvertColor.ps",
				InputFile = "images/test2.png"
			},
			new TestCase ("bright-extract") {
				Effect = "shaders/BrightExtract.ps",
				InputFile = "images/test2.png",
				CreateShader = (tc) => new SingleScalarShader (tc.Effect) { Scalar = 0.5 },
				ExtraArgs = "-c0=0.5"
			},
		};
	}

	public class TestCase {
		public TestCase (string testName) {
			TestName = testName;
		}
		public const double DefaultTolerance = 0.5;

		public double Tolerance { get; set; }
		public string TestName { get; set; }
		public string Effect { get; set; }
		public string InputFile { get; set; }
		public Func<TestCase, Shader> CreateShader { get; set; }
		public string ExtraArgs { get; set; }

		public void Run (TextWriter runscript) {
			Shader shader;
			if (CreateShader != null)
				shader = CreateShader (this);
			else
				shader = new Shader (Effect);

			BitmapImage bitmap = new BitmapImage (new Uri (Driver.MakePath (InputFile)));

			string reference = string.Format ("references/{0}.png", TestName);

			double t = Tolerance == 0 ? DefaultTolerance : Tolerance;

			Apply (shader, bitmap, reference);
			runscript.WriteLine ("{0} {1} {2} {3} {4} {5} {6} {7}", TestName, Effect, InputFile, reference, t, ExtraArgs);
		}

		static void Apply (Shader shader, BitmapImage bitmap, string destImage) {
			Rectangle r = new Rectangle ();
			r.Effect = shader;
			shader.Input = new ImageBrush (bitmap);
			/*The fill brush is ignored due to the effect been applied*/
			r.Fill = new SolidColorBrush (Colors.Aquamarine);

			Size size = new Size (bitmap.PixelWidth, bitmap.PixelHeight);
			r.Measure (size);
			r.Arrange (new Rect (size));

			RenderTargetBitmap render = new RenderTargetBitmap (
				bitmap.PixelWidth,
				bitmap.PixelHeight,
				96,
				96,
				PixelFormats.Pbgra32);

			render.Render (r);

			PngBitmapEncoder png = new PngBitmapEncoder ();
			png.Frames.Add (BitmapFrame.Create (render));
			using (Stream stm = File.Open (destImage, FileMode.OpenOrCreate)) {
				png.Save (stm);
			}
		}
	}

	public class Driver
	{
		public static string MakePath (string rel) {
			return Environment.CurrentDirectory + System.IO.Path.DirectorySeparatorChar + rel;
		}

		[STAThread]
		static void Main (string[] args) {
			using (StreamWriter sw = new StreamWriter ("tests.in")) {
				foreach (var test in TestSuite.Tests)
					test.Run (sw);
			}
		}
	}
}
