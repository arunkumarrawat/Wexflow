﻿using SkiaSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Versioning;
using System.Xml.Linq;
using Wexflow.Core;

namespace Wexflow.Tasks.ImagesConcat
{
    [SupportedOSPlatform("windows")]
    public class ImagesConcat : Task
    {
        public ImagesConcat(XElement xe, Workflow wf)
          : base(xe, wf)
        {
        }

        public override TaskStatus Run()
        {
            Workflow.CancellationTokenSource.Token.ThrowIfCancellationRequested();          
            Info("Concatenating images...");
            var status = Status.Success;

            try
            {
                var imageFiles = SelectFiles();

                if (imageFiles.Length >= 2)
                {
                    var extension = Path.GetExtension(imageFiles[0].FileName);
                    var destPath = Path.Combine(Workflow.WorkflowTempFolder,
                        $"ImagesConcat_{DateTime.Now:yyyy-MM-dd-HH-mm-ss-fff}{extension}");

                    var res = ConcatImages(imageFiles, destPath);
                    if (!res)
                    {
                        status = Status.Error;
                    }
                }
                else if (imageFiles.Length == 1)
                {
                    Error("You must provide at least two images to concatenate them.");
                }
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception e)
            {
                ErrorFormat("An error occured while concatenating images: {0}", e.Message);
                status = Status.Error;
            }

            Info("Task finished");
            return new TaskStatus(status);
        }

        private bool ConcatImages(FileInf[] imageFiles, string destPath)
        {
            try
            {
                List<int> imageHeights = [];
                var nIndex = 0;
                var width = 0;
                foreach (var imageFile in imageFiles)
                {
                    var img = SKImage.FromEncodedData(imageFile.Path);
                    imageHeights.Add(img.Height);
                    width += img.Width;
                }
                imageHeights.Sort();

                var height = imageHeights[^1];

                var imageInfo = new SKImageInfo(width, height);
                using (var surface = SKSurface.Create(imageInfo))
                {
                    var canvas = surface.Canvas;

                    foreach (var imageFile in imageFiles)
                    {
                        Workflow.CancellationTokenSource.Token.ThrowIfCancellationRequested();
                        var image = SKImage.FromEncodedData(imageFile.Path);
                        var bitmap = SKBitmap.FromImage(image);
                        if (nIndex == 0)
                        {
                            canvas.DrawBitmap(bitmap, new SKPoint(0, 0));
                            nIndex++;
                            width = image.Width;
                        }
                        else
                        {
                            canvas.DrawBitmap(bitmap, new SKPoint(width, 0));
                            width += image.Width;
                        }
                        if (!Workflow.CancellationTokenSource.Token.IsCancellationRequested)
                        {
                            WaitOne();
                        }
                        nIndex++;
                    }

                    using var img = surface.Snapshot();
                    using var data = img.Encode();
                    File.WriteAllBytes(destPath, data.ToArray());
                }

                InfoFormat("Image concatenation succeeded: {0}", destPath);
                Files.Add(new FileInf(destPath, Id));

                return true;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception e)
            {
                ErrorFormat("An error occured while concatenating images: {0}", e.Message);
                return false;
            }
        }
    }
}
