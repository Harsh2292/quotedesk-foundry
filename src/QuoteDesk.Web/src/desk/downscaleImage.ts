/**
 * Turns a picked photo into a JPEG data URL no larger than `maxEdge` pixels on its longest side,
 * before it ever enters React state. A phone photo is several MB; downscaled to ~1280px at 0.8
 * quality it is typically 100–300 KB — well under the API's 2 MB cap, and plenty for a model to read
 * handwriting from.
 */
export async function downscaleImage(file: File, maxEdge = 1280, quality = 0.8): Promise<string> {
  if (!file.type.startsWith('image/')) {
    throw new Error('Choose an image file (a photo of the enquiry).')
  }

  const source = await readAsDataUrl(file)
  const image = await loadImage(source)

  const scale = Math.min(1, maxEdge / Math.max(image.naturalWidth, image.naturalHeight))
  const width = Math.max(1, Math.round(image.naturalWidth * scale))
  const height = Math.max(1, Math.round(image.naturalHeight * scale))

  const canvas = document.createElement('canvas')
  canvas.width = width
  canvas.height = height
  const context = canvas.getContext('2d')
  if (!context) {
    throw new Error('This browser cannot process images.')
  }

  // JPEG has no transparency — paint white first so a transparent PNG does not turn black.
  context.fillStyle = '#ffffff'
  context.fillRect(0, 0, width, height)
  context.drawImage(image, 0, 0, width, height)
  return canvas.toDataURL('image/jpeg', quality)
}

function readAsDataUrl(file: File): Promise<string> {
  return new Promise((resolve, reject) => {
    const reader = new FileReader()
    reader.onload = () =>
      typeof reader.result === 'string'
        ? resolve(reader.result)
        : reject(new Error('Could not read the image.'))
    reader.onerror = () => reject(new Error('Could not read the image.'))
    reader.readAsDataURL(file)
  })
}

function loadImage(src: string): Promise<HTMLImageElement> {
  return new Promise((resolve, reject) => {
    const image = new Image()
    image.onload = () => resolve(image)
    image.onerror = () => reject(new Error('That file could not be opened as an image.'))
    image.src = src
  })
}
