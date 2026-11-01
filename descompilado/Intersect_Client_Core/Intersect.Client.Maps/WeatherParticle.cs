using System;
using System.Collections.Generic;
using Intersect.Client.Core;
using Intersect.Client.Entities;
using Intersect.Client.Framework.GenericClasses;
using Intersect.Client.Framework.Maps;
using Intersect.Client.General;
using Intersect.Enums;
using Intersect.Framework.Core;
using Intersect.Framework.Core.GameObjects.Animations;

namespace Intersect.Client.Maps;

public class WeatherParticle : IWeatherParticle, IDisposable
{
	private readonly List<IWeatherParticle> _RemoveParticle;

	private readonly Animation animInstance;

	private readonly Rectangle bounds;

	private readonly float cameraSpawnX;

	private readonly float cameraSpawnY;

	private readonly int originalX;

	private readonly int originalY;

	private readonly Point partSize;

	private readonly long TransmittionTimer;

	private readonly int xVelocity;

	private readonly int yVelocity;

	public float X { get; set; }

	public float Y { get; set; }

	public WeatherParticle(List<IWeatherParticle> RemoveParticle, int xvelocity, int yvelocity, AnimationDescriptor anim)
	{
		TransmittionTimer = Timing.Global.MillisecondsUtc;
		bounds = new Rectangle(0, 0, Graphics.Renderer.ScreenWidth, Graphics.Renderer.ScreenHeight);
		xVelocity = xvelocity;
		yVelocity = yvelocity;
		animInstance = new Animation(anim, loopForever: true);
		Point point = (partSize = animInstance.CalculateAnimationSize());
		if (xVelocity > 0)
		{
			originalX = Globals.Random.Next(-Graphics.Renderer.ScreenWidth / 4 - point.X, Graphics.Renderer.ScreenWidth + point.X);
		}
		else if (xVelocity < 0)
		{
			originalX = Globals.Random.Next(point.X, (int)((float)Graphics.Renderer.ScreenWidth * 1.25f) + point.X);
		}
		else
		{
			originalX = Globals.Random.Next(-point.X, Graphics.Renderer.ScreenWidth + point.X);
		}
		if (yVelocity > 0)
		{
			originalY = -point.Y;
		}
		else if (yVelocity < 0)
		{
			originalY = Graphics.Renderer.ScreenHeight + point.Y;
		}
		else
		{
			originalY = Globals.Random.Next(-point.Y, Graphics.Renderer.ScreenHeight + point.Y);
			if (xVelocity > 0)
			{
				originalX = -point.X;
			}
			else if (xVelocity < 0)
			{
				originalX = Graphics.Renderer.ScreenWidth;
			}
		}
		if (originalX < 0)
		{
			bounds.X = originalX;
			bounds.Width += Math.Abs(originalX);
		}
		if (originalY < 0)
		{
			bounds.Y = originalY;
			bounds.Height += Math.Abs(originalY);
		}
		if (originalY > Graphics.Renderer.ScreenHeight)
		{
			bounds.Height = originalY;
		}
		if (originalX > Graphics.Renderer.ScreenWidth)
		{
			bounds.Width = originalX;
		}
		bounds.X -= point.X;
		bounds.Width += point.X * 2;
		bounds.Y -= point.Y;
		bounds.Height += point.Y * 2;
		X = originalX;
		Y = originalY;
		cameraSpawnX = Graphics.Renderer.GetView().Left;
		cameraSpawnY = Graphics.Renderer.GetView().Top;
		_RemoveParticle = RemoveParticle;
	}

	public void Update()
	{
		Rectangle rectangle = new Rectangle(bounds.X + ((int)Graphics.Renderer.GetView().Left - (int)cameraSpawnX), bounds.Y + ((int)Graphics.Renderer.GetView().Top - (int)cameraSpawnY), bounds.Width, bounds.Height);
		if (!rectangle.IntersectsWith(new Rectangle((int)X, (int)Y, partSize.X, partSize.Y)))
		{
			if (_RemoveParticle.Contains(this))
			{
				throw new Exception();
			}
			_RemoveParticle.Add(this);
		}
		else
		{
			float num = (float)(Timing.Global.MillisecondsUtc - TransmittionTimer) / 10f;
			X = (float)originalX + (float)xVelocity * num;
			Y = (float)originalY + (float)yVelocity * num;
			animInstance.SetPosition(cameraSpawnX + X, cameraSpawnY + Y, -1, -1, Guid.Empty, Direction.None);
			animInstance.Update();
		}
	}

	public void Dispose()
	{
		Dispose(disposing: true);
		GC.SuppressFinalize(this);
	}

	private void Dispose(bool disposing)
	{
		ReleaseUnmanagedResources();
		if (disposing)
		{
			animInstance.Dispose();
			ReleaseManagedResources();
		}
	}

	protected virtual void ReleaseManagedResources()
	{
	}

	protected virtual void ReleaseUnmanagedResources()
	{
	}

	~WeatherParticle()
	{
		Dispose(disposing: false);
	}
}
