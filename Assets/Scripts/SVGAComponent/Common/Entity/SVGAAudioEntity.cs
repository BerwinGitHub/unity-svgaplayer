namespace Bo.SVGA
{
    public class SVGAAudioEntity
    {
        public SVGAAudioData SvgaAudioData { get; set; }
        public AudioEntity AudioEntity { get; set; }

        public SVGAAudioEntity(AudioEntity audioEntity, SVGAAudioData svgaAudioData)
        {
            SvgaAudioData = svgaAudioData;
            AudioEntity = audioEntity;
        }
    }
}