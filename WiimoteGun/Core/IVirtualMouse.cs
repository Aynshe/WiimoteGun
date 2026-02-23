using System;

namespace WiimoteGun
{
    /// <summary>
    /// Interface for virtual mouse implementations (EN/FR: Interface pour les implémentations de souris virtuelle)
    /// </summary>
    public interface IVirtualMouse : IDisposable
    {
        /// <param name="isAbsolute">Whether the coordinates are absolute (65535 range) or relative (EN/FR: Si les coordonnées sont absolues ou relatives)</param>
        void UpdateMouse(int x, int y, bool leftButton, bool rightButton, bool middleButton, bool moveCursor = true, bool isAbsolute = true);
    }
}
