using System;

namespace WiimoteGun
{
    /// <summary>
    /// Interface for virtual mouse implementations (EN/FR: Interface pour les implémentations de souris virtuelle)
    /// </summary>
    public interface IVirtualMouse : IDisposable
    {
        /// <summary>
        /// Update mouse position and button states (EN/FR: Mettre à jour la position et les boutons de la souris)
        /// </summary>
        void UpdateMouse(int x, int y, bool leftButton, bool rightButton, bool middleButton, bool moveCursor = true);
    }
}
