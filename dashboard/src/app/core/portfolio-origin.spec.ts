import { isApiHostedImage, isCloudinaryImage, resolvePortfolioImage } from './portfolio-origin';

describe('portfolio-origin image resolution', () => {
  describe('resolvePortfolioImage', () => {
    it('returns a Cloudinary URL untouched — the CDN owns resizing', () => {
      const cloudinary = 'https://res.cloudinary.com/demo/image/upload/v1737500000/portfolio/projects/haxel/cover.webp';

      expect(resolvePortfolioImage(cloudinary, 640)).toBe(cloudinary);
      expect(resolvePortfolioImage(cloudinary, 1280)).toBe(cloudinary);
    });

    it('returns a transformed Cloudinary URL untouched (no double transformation)', () => {
      const transformed = 'https://res.cloudinary.com/demo/image/upload/f_auto,q_auto,w_640/v2/portfolio/projects/haxel/cover.webp';

      expect(resolvePortfolioImage(transformed)).toBe(transformed);
    });

    it('keeps a legacy /api/files cover as an API-served path (backward compatible)', () => {
      expect(resolvePortfolioImage('/api/files/haxel-footwear-1280.webp')).toBe('/api/files/haxel-footwear-1280.webp');
    });

    it('falls back to the placeholder when there is no cover', () => {
      expect(resolvePortfolioImage(null)).toBe('/placeholder.svg');
      expect(resolvePortfolioImage(undefined)).toBe('/placeholder.svg');
    });

    it('renders absolute non-Cloudinary URLs as-is', () => {
      const external = 'https://avatars.githubusercontent.com/u/1?v=4';

      expect(resolvePortfolioImage(external)).toBe(external);
    });
  });

  describe('isCloudinaryImage', () => {
    it('detects Cloudinary delivery URLs and rejects everything else', () => {
      expect(isCloudinaryImage('https://res.cloudinary.com/demo/image/upload/v1/a/b.png')).toBeTrue();
      expect(isCloudinaryImage('https://api.example.com/image/upload/v1/a.png')).toBeFalse();
      expect(isCloudinaryImage('http://res.cloudinary.com/demo/image/upload/v1/a.png')).toBeFalse();
      expect(isCloudinaryImage('/api/files/cover-1280.webp')).toBeFalse();
      expect(isCloudinaryImage('https://example.com/cover.png')).toBeFalse();
      expect(isCloudinaryImage(null)).toBeFalse();
    });
  });

  describe('isApiHostedImage', () => {
    it('flags API-hosted covers so upload problems can warn the admin', () => {
      expect(isApiHostedImage('/api/files/cover-1280.webp')).toBeTrue();
      expect(isApiHostedImage('/api/files/legacy.png')).toBeTrue();
    });

    it('never flags Cloudinary, bundled or missing covers', () => {
      expect(isApiHostedImage('https://res.cloudinary.com/demo/image/upload/v1/a.png')).toBeFalse();
      expect(isApiHostedImage('/projects/react/HAVEN.png')).toBeFalse();
      expect(isApiHostedImage(null)).toBeFalse();
    });
  });
});
