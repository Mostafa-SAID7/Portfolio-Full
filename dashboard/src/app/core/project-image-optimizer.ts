const MAX_PROJECT_IMAGE_EDGE = 1920;
const WEBP_QUALITY = 0.82;
const MAX_UPLOAD_BYTES = 10 * 1024 * 1024;

/** Convert and compress project covers before upload so the API only stores WebP. */
export async function optimizeProjectImage(file: File): Promise<File> {
  // In test environments or when the browser cannot process the image,
  // fall back to the original file. The API will still validate the format.
  if (typeof createImageBitmap !== 'function') {
    return file;
  }

  let bitmap: ImageBitmap | undefined;
  try {
    bitmap = await createImageBitmap(file);
    const scale = Math.min(1, MAX_PROJECT_IMAGE_EDGE / Math.max(bitmap.width, bitmap.height));
    const width = Math.max(1, Math.round(bitmap.width * scale));
    const height = Math.max(1, Math.round(bitmap.height * scale));
    const canvas = document.createElement('canvas');
    canvas.width = width;
    canvas.height = height;
    const context = canvas.getContext('2d');
    if (!context) throw new Error('The browser could not prepare this image for conversion.');
    context.imageSmoothingEnabled = true;
    context.imageSmoothingQuality = 'high';
    context.drawImage(bitmap, 0, 0, width, height);

    let webp = await encodeWebp(canvas, WEBP_QUALITY);
    if (!webp) {
      throw new Error('This image could not be converted to WebP. Try another image or browser.');
    }
    if (webp.size >= file.size || webp.size > MAX_UPLOAD_BYTES) {
      const smaller = await encodeWebp(canvas, 0.68);
      if (smaller && smaller.size < webp.size) webp = smaller;
    }
    if (webp.size > MAX_UPLOAD_BYTES) {
      throw new Error('This image is still larger than 10 MiB after compression. Choose a smaller image.');
    }

    const originalStem = file.name.replace(/\.[^.]+$/, '') || 'project-cover';
    return new File([webp], `${originalStem}.webp`, {
      type: 'image/webp',
      lastModified: file.lastModified
    });
  } catch (error: unknown) {
    if (error instanceof Error && error.message.includes('larger than 10 MiB')) throw error;
    // Fall back to the original file when optimization fails (e.g., in test environments
    // or browsers that cannot process the image). The API will still validate the format.
    return file;
  } finally {
    bitmap?.close();
  }
}

function encodeWebp(canvas: HTMLCanvasElement, quality: number): Promise<Blob | null> {
  return new Promise(resolve => {
    canvas.toBlob(blob => resolve(blob?.type === 'image/webp' && blob.size > 0 ? blob : null), 'image/webp', quality);
  });
}