// React Imports
import { useContext } from 'react'

// Type Imports
import type { VerticalMenuContextProps } from '../components/vertical-menu/Menu'

// Context Imports
import { VerticalMenuContext } from '../components/vertical-menu/Menu'

const useVerticalMenu = (): VerticalMenuContextProps => {
  // Hooks
  const context = useContext(VerticalMenuContext)

  if (context === undefined) {
    throw new Error('useVerticalMenu must be used within a VerticalMenuProvider.')
  }

  return context
}

export default useVerticalMenu
