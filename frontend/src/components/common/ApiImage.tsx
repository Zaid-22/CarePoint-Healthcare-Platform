import { useEffect, useState, type ImgHTMLAttributes } from 'react';
import api from '../../api/client';
import { resolveApiAssetUrl } from '../../utils/apiAssets';

type ApiImageProps = Omit<ImgHTMLAttributes<HTMLImageElement>, 'src'> & {
  source?: string | null;
  fallback: string;
  authenticated?: boolean;
};

export default function ApiImage({
  source,
  fallback,
  authenticated = false,
  onError,
  ...imageProps
}: ApiImageProps) {
  const [imageSource, setImageSource] = useState(fallback);

  useEffect(() => {
    const requestedSource = source?.trim();
    if (!requestedSource) {
      setImageSource(fallback);
      return;
    }

    const resolvedSource = resolveApiAssetUrl(requestedSource);
    if (!authenticated || !requestedSource.startsWith('/api/')) {
      setImageSource(resolvedSource);
      return;
    }

    const controller = new AbortController();
    let objectUrl: string | null = null;
    setImageSource(fallback);

    api.get<Blob>(resolvedSource, {
      responseType: 'blob',
      signal: controller.signal,
    }).then((response) => {
      if (controller.signal.aborted) return;
      objectUrl = URL.createObjectURL(response.data);
      setImageSource(objectUrl);
    }).catch((error: unknown) => {
      if (!controller.signal.aborted) {
        console.error('Failed to load protected profile image', error);
        setImageSource(fallback);
      }
    });

    return () => {
      controller.abort();
      if (objectUrl) URL.revokeObjectURL(objectUrl);
    };
  }, [authenticated, fallback, source]);

  return (
    <img
      {...imageProps}
      src={imageSource}
      onError={(event) => {
        if (imageSource !== fallback) setImageSource(fallback);
        onError?.(event);
      }}
    />
  );
}
