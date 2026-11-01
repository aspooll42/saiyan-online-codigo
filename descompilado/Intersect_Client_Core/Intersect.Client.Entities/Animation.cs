using System;
using Intersect.Client.Core;
using Intersect.Client.Core.Sounds;
using Intersect.Client.Framework.Content;
using Intersect.Client.Framework.Entities;
using Intersect.Client.Framework.GenericClasses;
using Intersect.Client.Framework.Graphics;
using Intersect.Client.General;
using Intersect.Enums;
using Intersect.Framework.Core;
using Intersect.Framework.Core.GameObjects.Animations;
using Intersect.Framework.Core.GameObjects.Lighting;

namespace Intersect.Client.Entities;

public class Animation : IAnimation, IDisposable
{
	private bool _disposed;

	private bool mDisposeNextDraw;

	private int mLowerFrame;

	private readonly int mLowerLoop;

	private readonly IEntity? mParent;

	private Direction mRenderDir;

	private float mRenderX;

	private float mRenderY;

	private bool mShowLower = true;

	private bool mShowUpper = true;

	private MapSound? mSound;

	private readonly long mStartTime = Timing.Global.MillisecondsUtc;

	private int mUpperFrame;

	private readonly int mUpperLoop;

	private bool mUseExternalRotation;

	private float mExternalRotation;

	private int mZDimension = -1;

	public bool AutoRotate { get; set; }

	public bool Hidden { get; set; }

	public bool SousLesCimes { get; set; }

	private bool SousLesCimesIci
	{
		get
		{
			if (SousLesCimes)
			{
				if (mParent is Entity entity)
				{
					return !Flight.IsAirborne(entity);
				}
				return true;
			}
			return false;
		}
	}

	public bool InfiniteLoop { get; set; }

	public AnimationDescriptor? Descriptor { get; set; }

	public AnimationSource Source { get; }

	public Point Size => CalculateAnimationSize();

	public bool IsDisposed => _disposed;

	public event Action<IAnimation>? Disposed;

	public event Action<IAnimation>? Finished;

	public Animation(AnimationDescriptor animationDescriptor, bool loopForever, bool autoRotate = false, int zDimension = -1, IEntity? parent = null, AnimationSource source = default(AnimationSource))
	{
		Descriptor = animationDescriptor;
		Source = source;
		mParent = parent;
		if (Descriptor != null)
		{
			mLowerLoop = animationDescriptor.Lower.LoopCount;
			mUpperLoop = animationDescriptor.Upper.LoopCount;
			InfiniteLoop = loopForever;
			AutoRotate = autoRotate;
			mZDimension = zDimension;
			mSound = Audio.AddMapSound(Descriptor.Sound, 0, 0, Guid.Empty, loopForever, 0, 12, parent);
			lock (Graphics.AnimationLock)
			{
				Graphics.LiveAnimations.Add(this);
				return;
			}
		}
		Dispose();
	}

	public void Draw(bool upper = false, bool alternate = false)
	{
		if (Hidden || Descriptor == null || (!upper && alternate != Descriptor.Lower.AlternateRenderLayer) || (upper && alternate != (SousLesCimesIci || Descriptor.Upper.AlternateRenderLayer)))
		{
			return;
		}
		float num = 0f;
		bool flag = (upper && Descriptor.Upper.DisableRotations) || (!upper && Descriptor.Lower.DisableRotations);
		if (mUseExternalRotation)
		{
			num = mExternalRotation;
		}
		if ((AutoRotate || mRenderDir != Direction.None) && !flag && !mUseExternalRotation)
		{
			switch (mRenderDir)
			{
			case Direction.Up:
				num = 0f;
				break;
			case Direction.Down:
				num = 180f;
				break;
			case Direction.Left:
				num = 270f;
				break;
			case Direction.Right:
				num = 90f;
				break;
			case Direction.UpLeft:
				num = 315f;
				break;
			case Direction.UpRight:
				num = 45f;
				break;
			case Direction.DownRight:
				num = 135f;
				break;
			case Direction.DownLeft:
				num = 225f;
				break;
			}
		}
		if ((!upper && mShowLower && mZDimension < 1) || (!upper && mShowLower && mZDimension > 0))
		{
			IGameTexture texture = Globals.ContentManager.GetTexture(TextureType.Animation, Descriptor.Lower.Sprite);
			if (texture != null && Descriptor.Lower.XFrames > 0 && Descriptor.Lower.YFrames > 0)
			{
				int num2 = texture.Width / Descriptor.Lower.XFrames;
				int num3 = texture.Height / Descriptor.Lower.YFrames;
				Graphics.DrawGameTexture(texture, new FloatRect(mLowerFrame % Descriptor.Lower.XFrames * num2, (float)Math.Floor((double)mLowerFrame / (double)Descriptor.Lower.XFrames) * (float)num3, num2, num3), new FloatRect(mRenderX - (float)(num2 / 2), mRenderY - (float)(num3 / 2), num2, num3), Color.White, null, GameBlendModes.None, null, num);
			}
			int offsetX = Descriptor.Lower.Lights[mLowerFrame].OffsetX;
			int offsetY = Descriptor.Lower.Lights[mLowerFrame].OffsetY;
			Point point = RotatePoint(new Point(offsetX, offsetY), new Point(0, 0), num + 180f);
			Graphics.AddLight((int)mRenderX - point.X, (int)mRenderY - point.Y, Descriptor.Lower.Lights[mLowerFrame].Size, Descriptor.Lower.Lights[mLowerFrame].Intensity, Descriptor.Lower.Lights[mLowerFrame].Expand, Descriptor.Lower.Lights[mLowerFrame].Color);
		}
		if ((upper && mShowUpper && mZDimension != 0) || (upper && mShowUpper && mZDimension == 0))
		{
			IGameTexture texture2 = Globals.ContentManager.GetTexture(TextureType.Animation, Descriptor.Upper.Sprite);
			if (texture2 != null && Descriptor.Upper.XFrames > 0 && Descriptor.Upper.YFrames > 0)
			{
				int num4 = texture2.Width / Descriptor.Upper.XFrames;
				int num5 = texture2.Height / Descriptor.Upper.YFrames;
				Graphics.DrawGameTexture(texture2, new FloatRect(mUpperFrame % Descriptor.Upper.XFrames * num4, (float)Math.Floor((double)mUpperFrame / (double)Descriptor.Upper.XFrames) * (float)num5, num4, num5), new FloatRect(mRenderX - (float)(num4 / 2), mRenderY - (float)(num5 / 2), num4, num5), Color.White, null, GameBlendModes.None, null, num);
			}
			int offsetX2 = Descriptor.Upper.Lights[mUpperFrame].OffsetX;
			int offsetY2 = Descriptor.Upper.Lights[mUpperFrame].OffsetY;
			Point point2 = RotatePoint(new Point(offsetX2, offsetY2), new Point(0, 0), num + 180f);
			Graphics.AddLight((int)mRenderX - point2.X, (int)mRenderY - point2.Y, Descriptor.Upper.Lights[mUpperFrame].Size, Descriptor.Upper.Lights[mUpperFrame].Intensity, Descriptor.Upper.Lights[mUpperFrame].Expand, Descriptor.Upper.Lights[mUpperFrame].Color);
		}
	}

	public void EndDraw()
	{
		if (mDisposeNextDraw)
		{
			Dispose();
		}
		this.Finished?.Invoke(this);
	}

	private static Point RotatePoint(Point pointToRotate, Point centerPoint, double angleInDegrees)
	{
		double num = angleInDegrees * (Math.PI / 180.0);
		double num2 = Math.Cos(num);
		double num3 = Math.Sin(num);
		return new Point
		{
			X = (int)(num2 * (double)(pointToRotate.X - centerPoint.X) - num3 * (double)(pointToRotate.Y - centerPoint.Y) + (double)centerPoint.X),
			Y = (int)(num3 * (double)(pointToRotate.X - centerPoint.X) + num2 * (double)(pointToRotate.Y - centerPoint.Y) + (double)centerPoint.Y)
		};
	}

	public void Hide()
	{
		Hidden = true;
	}

	public void Show()
	{
		Hidden = false;
	}

	public bool ParentGone()
	{
		return mParent?.IsDisposed ?? false;
	}

	public void Dispose()
	{
		ObjectDisposedException.ThrowIf(_disposed, this);
		lock (Graphics.AnimationLock)
		{
			if (mSound != null)
			{
				mSound.Loop = false;
				AnimationDescriptor? descriptor = Descriptor;
				if (descriptor != null && !descriptor.CompleteSound)
				{
					mSound.Stop();
				}
				mSound = null;
			}
			Graphics.LiveAnimations.Remove(this);
			_disposed = true;
			GC.SuppressFinalize(this);
		}
		this.Disposed?.Invoke(this);
	}

	public void DisposeNextDraw()
	{
		mDisposeNextDraw = true;
	}

	public void SetPosition(float worldX, float worldY, int mapx, int mapy, Guid mapId, Direction dir, int z = 0)
	{
		mRenderX = worldX;
		mRenderY = worldY;
		mSound?.UpdatePosition(mapx, mapy, mapId);
		if (dir > Direction.None)
		{
			mRenderDir = dir;
		}
		mZDimension = z;
	}

	public void Update()
	{
		if (_disposed || Descriptor == null)
		{
			return;
		}
		if (mSound != null)
		{
			mSound.Update();
		}
		long num = Timing.Global.MillisecondsUtc - mStartTime;
		if (Descriptor.Lower.FrameCount > 0 && Descriptor.Lower.FrameSpeed > 0)
		{
			int num2 = Math.Min(Descriptor.Lower.FrameCount, Descriptor.Lower.XFrames * Descriptor.Lower.YFrames);
			int num3 = (int)Math.Floor((float)num / (float)Descriptor.Lower.FrameSpeed);
			int num4 = (int)Math.Floor((float)num3 / (float)num2);
			if (num4 > mLowerLoop && !InfiniteLoop)
			{
				mShowLower = false;
			}
			else
			{
				mLowerFrame = num3 - num4 * num2;
			}
		}
		if (Descriptor.Upper.FrameCount > 0 && Descriptor.Upper.FrameSpeed > 0)
		{
			int num5 = Math.Min(Descriptor.Upper.FrameCount, Descriptor.Upper.XFrames * Descriptor.Upper.YFrames);
			int num6 = (int)Math.Floor((float)num / (float)Descriptor.Upper.FrameSpeed);
			int num7 = (int)Math.Floor((float)num6 / (float)num5);
			if (num7 > mUpperLoop && !InfiniteLoop)
			{
				mShowUpper = false;
			}
			else
			{
				mUpperFrame = num6 - num7 * num5;
			}
		}
		if (!mShowLower && !mShowUpper)
		{
			Dispose();
		}
	}

	public Point CalculateAnimationSize()
	{
		Point result = new Point(0, 0);
		if (Descriptor == null)
		{
			return result;
		}
		IGameTexture texture = Globals.ContentManager.GetTexture(TextureType.Animation, Descriptor.Lower.Sprite);
		if (texture != null && Descriptor.Lower.XFrames > 0 && Descriptor.Lower.YFrames > 0)
		{
			int num = texture.Width / Descriptor.Lower.XFrames;
			int num2 = texture.Height / Descriptor.Lower.YFrames;
			if (num > result.X)
			{
				result.X = num;
			}
			if (num2 > result.Y)
			{
				result.Y = num2;
			}
		}
		texture = Globals.ContentManager.GetTexture(TextureType.Animation, Descriptor.Upper.Sprite);
		if (texture != null && Descriptor.Upper.XFrames > 0 && Descriptor.Upper.YFrames > 0)
		{
			int num3 = texture.Width / Descriptor.Upper.XFrames;
			int num4 = texture.Height / Descriptor.Upper.YFrames;
			if (num3 > result.X)
			{
				result.X = num3;
			}
			if (num4 > result.Y)
			{
				result.Y = num4;
			}
		}
		LightDescriptor[] lights = Descriptor.Lower.Lights;
		foreach (LightDescriptor lightDescriptor in lights)
		{
			if (lightDescriptor != null)
			{
				if (lightDescriptor.Size + Math.Abs(lightDescriptor.OffsetX) > result.X)
				{
					result.X = lightDescriptor.Size + lightDescriptor.OffsetX;
				}
				if (lightDescriptor.Size + Math.Abs(lightDescriptor.OffsetY) > result.Y)
				{
					result.Y = lightDescriptor.Size + lightDescriptor.OffsetY;
				}
			}
		}
		LightDescriptor[] lights2 = Descriptor.Upper.Lights;
		foreach (LightDescriptor lightDescriptor2 in lights2)
		{
			if (lightDescriptor2 != null)
			{
				if (lightDescriptor2.Size + Math.Abs(lightDescriptor2.OffsetX) > result.X)
				{
					result.X = lightDescriptor2.Size + lightDescriptor2.OffsetX;
				}
				if (lightDescriptor2.Size + Math.Abs(lightDescriptor2.OffsetY) > result.Y)
				{
					result.Y = lightDescriptor2.Size + lightDescriptor2.OffsetY;
				}
			}
		}
		return result;
	}

	public void SetDir(Direction dir)
	{
		mRenderDir = dir;
	}

	public void SetRotation(float angleInDegrees)
	{
		mUseExternalRotation = true;
		mExternalRotation = angleInDegrees;
	}

	public void SetRotation(bool toggle)
	{
		mUseExternalRotation = toggle;
	}
}
