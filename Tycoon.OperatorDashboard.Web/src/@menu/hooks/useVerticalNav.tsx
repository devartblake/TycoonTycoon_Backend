// React Imports
import { useContext } from 'react'

// Context Imports
import VerticalNavContext from '../contexts/verticalNavContext'

const useVerticalNav = () => {
  // Hooks
  const context = useContext(VerticalNavContext)

  if (context === undefined) {
    throw new Error('useVerticalNav must be used within a VerticalNavProvider.')
  }

  return context
}

export default useVerticalNav
