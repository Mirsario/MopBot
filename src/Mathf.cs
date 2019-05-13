using System;

namespace MopBotTwo
{
	public static class Mathf
	{
		public const float NegativeInfinity = float.NegativeInfinity;
		public const float Infinity = float.PositiveInfinity;
		public const float PI = 3.14159265358979323846264f;
		public const float Deg2Rad = 0.01745329251994329576924f;
		public const float Rad2Deg = 57.2957795130823208768f;

		public const float Epsilon = 1.401298E-45f;

		public static float StepTowards(float val,float goal,float step)
		{
			if(goal>val) {
				val += step;
				if(val>goal) {
					val = goal;
				}
			} else if(goal<val) {
				val -= step;
				if(val<goal) {
					val = goal;
				}
			}
			return val;
		}
		public static float Repeat(float t,float length)
		{
			return t-Floor(t/length)*length;
		}
		public static float Sin01(float f)
		{
			return Sin(f*PI);
		}
		public static float Sin(float f)
		{
			return (float)Math.Sin(f);
		}
		public static float Cos(float f)
		{
			return (float)Math.Cos(f);
		}
		public static float Tan(float f)
		{
			return (float)Math.Tan(f);
		}
		public static float Asin(float f)
		{
			return (float)Math.Asin(f);
		}
		public static float Acos(float f)
		{
			return (float)Math.Acos(f);
		}
		public static float Atan(float f)
		{
			return (float)Math.Atan(f);
		}
		public static float Atan2(float y,float x)
		{
			return (float)Math.Atan2(y,x);
		}
		public static float Sqrt(float f)
		{
			return (float)Math.Sqrt(f);
		}
		public static float SqrtReciprocal(float f)
		{
			/*long i;
			float y,r;
			y=	f*0.5f;
			i=	(long)f;
			i=	0x5f3759df-(i>>1);
			r=	(float)i;
			r=	r*(1.5f-r*r*y);
			return r;*/
			return 1f/Mathf.Sqrt(f);
		}
		public static float Abs(float f)
		{
			return Math.Abs(f);
		}
		public static int Abs(int value)
		{
			return Math.Abs(value);
		}
		public static float Pow(float f,float p)
		{
			return (float)Math.Pow(f,p);
		}
		public static float Exp(float power)
		{
			return (float)Math.Exp(power);
		}
		public static float Log(float f,float p)
		{
			return (float)Math.Log(f,p);
		}
		public static float Log(float f)
		{
			return (float)Math.Log(f);
		}
		public static float Log10(float f)
		{
			return (float)Math.Log10(f);
		}
		public static float Ceil(float f)
		{
			return (float)Math.Ceiling(f);
		}
		public static float Floor(float f)
		{
			return (float)Math.Floor(f);
		}
		public static float Round(float f)
		{
			return (float)Math.Round(f);
		}
		public static int CeilToInt(float f)
		{
			return (int)Math.Ceiling(f);
		}
		public static int FloorToInt(float f)
		{
			return (int)Math.Floor(f);
		}
		public static int RoundToInt(float f)
		{
			return (int)Math.Round(f);
		}
		public static float Dot(float[] a,float[] b)
		{
			return a[0]*b[0]+a[1]*b[1];
		}
		public static float Sign(float f)
		{
			return f<0f ? -1f : 1f;
		}
		public static float Clamp(float value,float min,float max)
		{
			if(value<min) {
				value = min;
			} else if(value>max) {
				value = max;
			}
			return value;
		}
		public static double Clamp(double value,double min,double max)
		{
			if(value<min) {
				value = min;
			} else if(value>max) {
				value = max;
			}
			return value;
		}
		public static float Clamp01(float value)
		{
			if(value<0f) {
				return 0f;
			} else if(value>1f) {
				return 1f;
			}
			return value;
		}
		public static int Clamp(int value,int min,int max)
		{
			if(value<min) {
				value = min;
			} else if(value>max) {
				value = max;
			}
			return value;
		}
		//Min/Max
		public static float Min(float a,float b)
		{
			return a>=b ? b : a;
		}
		public static float Min(params float[] values)
		{
			if(values.Length==0) {
				return 0f;
			}
			float num = values[0];
			for(int i = 1;i<values.Length;i++) {
				if(values[i]<num) {
					num = values[i];
				}
			}
			return num;
		}
		public static int Min(int a,int b)
		{
			return a>=b ? b : a;
		}
		public static int Min(params int[] values)
		{
			if(values.Length==0) {
				return 0;
			}
			int num = values[0];
			for(int i = 1;i<values.Length;i++) {
				if(values[i]<num) {
					num = values[i];
				}
			}
			return num;
		}
		public static float Max(float a,float b)
		{
			return a<=b ? b : a;
		}
		public static float Max(params float[] values)
		{
			if(values.Length==0) {
				return 0f;
			}
			float num = values[0];
			for(int i = 1;i<values.Length;i++) {
				if(values[i]>num) {
					num = values[i];
				}
			}
			return num;
		}
		public static int Max(int a,int b)
		{
			return a<=b ? b : a;
		}
		public static int Max(params int[] values)
		{
			if(values.Length==0) {
				return 0;
			}
			int num = values[0];
			for(int i = 1;i<values.Length;i++) {
				if(values[i]>num) {
					num = values[i];
				}
			}
			return num;
		}

		public static float Lerp(float a,float b,float time)
		{
			return a+(b-a)*Clamp01(time);
		}
		public static float LerpAngle(float a,float b,float t)
		{
			float num = Repeat(b-a,360f);
			if(num>180f) {
				num -= 360f;
			}
			return a+num*Clamp01(t);
		}
	}
}