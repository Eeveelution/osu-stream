﻿using OpenTK;
using OpenTK.Graphics;
using osum.Helpers;

namespace osum.Graphics.Sprites
{
    internal enum LoopTypes
    {
        LoopForever,
        LoopOnce
    }

    internal class pAnimation : pSprite
    {
        private int currentFrame;
        public int[] CustomSequence;
        internal bool Reverse;

        internal double FrameDelay = 1000 / 60f;

        internal double FramesPerSecond
        {
            get => 1000 / FrameDelay;

            set => FrameDelay = 1000.0 / value;
        }

        public LoopTypes LoopType = LoopTypes.LoopForever;
        internal bool RunAnimation = true;

        private pTexture[] textureArray;

        internal int TextureCount;

        internal pAnimation(pTexture[] textures, FieldTypes fieldType, OriginTypes originType, ClockTypes clockType,
            Vector2 startPosition, float drawDepth, bool alwaysDraw, Color4 colour)
            : base(textures == null || textures.Length == 0 ? null : textures[0], fieldType, originType, clockType, startPosition, drawDepth, alwaysDraw, colour)
        {
            if (textures != null)
                TextureArray = textures;
        }

        internal int CurrentFrame
        {
            get => currentFrame;
            set => currentFrame = value;
        }

        internal pTexture[] TextureArray
        {
            get => textureArray;
            set
            {
                textureArray = value;


                if (textureArray != null)
                {
                    TextureCount = textureArray.Length;
                    currentFrame = 0;
                    if (TextureCount > 0)
                        texture = textureArray[0];
                    else
                        texture = null;
                }
            }
        }

        internal override pTexture Texture
        {
            get
            {
                if (TextureArray == null || TextureCount == 0)
                    return null;

                return texture;
            }
        }

        internal event VoidDelegate AnimationFinished;

        private void InvokeAnimationFinished()
        {
            VoidDelegate a = AnimationFinished;
            a?.Invoke();
        }

        private int lastFrame;

        private void resetAnimation()
        {
            currentFrame = 0;
        }

        public override void Update()
        {
            UpdateFrame();

            base.Update();
        }

        private double timeSinceLastFrame;

        private int lastFrameSpriteTime;

        internal void UpdateFrame()
        {
            if ((!RunAnimation && lastFrame == currentFrame))
                return;

            if (TextureCount < 2)
                return;

            int spriteTime = Clock.GetTime(Clocking);

            if (spriteTime == lastFrameSpriteTime)
                return; //no time has elapsed; the clocking is likely paused.

            lastFrameSpriteTime = spriteTime;

            timeSinceLastFrame += Clock.ElapsedMilliseconds;

            while (timeSinceLastFrame > FrameDelay)
            {
                increaseCurrentFrame(false);
                timeSinceLastFrame -= FrameDelay;
            }

            if (lastFrame != currentFrame)
            {
                Texture = hasCustomSequence ? TextureArray[CustomSequence[currentFrame]] : TextureArray[currentFrame];
            }

            lastFrame = currentFrame;
        }

        private void increaseCurrentFrame(bool reverse)
        {
            if (LoopType == LoopTypes.LoopOnce && currentFrame == maxFrame)
            {
                CustomSequence = null;
                InvokeAnimationFinished();
            }
            else
            {
                if (Reverse) reverse = !reverse;
                if (reverse)
                    currentFrame = (TextureCount + currentFrame - 1) % (maxFrame + 1);
                else
                    currentFrame = (currentFrame + 1) % (maxFrame + 1);
            }
        }

        internal void SetFramerateFromSkin()
        {
            /*
            if (SkinManager.Current == null || textureArray == null) return;

            if (SkinManager.Current.AnimationFramerate > 0)
                frameSkip = (1000f / SkinManager.Current.AnimationFramerate) / Constant.SIXTY_FRAME_TIME;
            else
                frameSkip = (1000f / TextureArray.Length) / Constant.SIXTY_FRAME_TIME;
            */
        }

        public override pDrawable Clone()
        {
            pAnimation clone = new pAnimation(TextureArray, Field, Origin, Clocking, Position, DrawDepth, AlwaysDraw, Colour);
            clone.FrameDelay = FrameDelay;

            foreach (Transformation t in Transformations)
                clone.Transform(t.Clone());
            return clone;
        }

        public bool hasCustomSequence => CustomSequence != null;

        public int maxFrame => !hasCustomSequence ? TextureCount - 1 : CustomSequence.Length - 1;
    }
}