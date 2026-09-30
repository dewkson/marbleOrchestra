namespace MarbleOrchestra.Grid
{
    /// <summary>
    /// Which 3D element a Trigger block shows (see 0055). Xylophone is the
    /// first value on purpose: it is the default of every content asset
    /// that was saved before this enum existed. New instruments: add a
    /// value here and a case in InstrumentBlockDecoration.
    /// </summary>
    public enum InstrumentType
    {
        Xylophone,
        Timpani,
        Snare,
        HiHat,
    }
}
