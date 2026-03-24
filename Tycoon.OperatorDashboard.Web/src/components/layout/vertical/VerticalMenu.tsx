// MUI Imports
import { useTheme } from '@mui/material/styles'

// Third-party Imports
import PerfectScrollbar from 'react-perfect-scrollbar'

// Type Imports
import type { VerticalMenuContextProps } from '@menu/components/vertical-menu/Menu'

// Component Imports
import { Menu, MenuItem, MenuSection } from '@menu/vertical-menu'

// Hook Imports
import useVerticalNav from '@menu/hooks/useVerticalNav'

// Styled Component Imports
import StyledVerticalNavExpandIcon from '@menu/styles/vertical/StyledVerticalNavExpandIcon'

// Style Imports
import menuItemStyles from '@core/styles/vertical/menuItemStyles'
import menuSectionStyles from '@core/styles/vertical/menuSectionStyles'

type RenderExpandIconProps = {
  open?: boolean
  transitionDuration?: VerticalMenuContextProps['transitionDuration']
}

const RenderExpandIcon = ({ open, transitionDuration }: RenderExpandIconProps) => (
  <StyledVerticalNavExpandIcon open={open} transitionDuration={transitionDuration}>
    <i className='ri-arrow-right-s-line' />
  </StyledVerticalNavExpandIcon>
)

const VerticalMenu = ({ scrollMenu }: { scrollMenu: (container: any, isPerfectScrollbar: boolean) => void }) => {
  // Hooks
  const theme = useTheme()
  const { isBreakpointReached, transitionDuration } = useVerticalNav()

  const ScrollWrapper = isBreakpointReached ? 'div' : PerfectScrollbar

  return (
    <ScrollWrapper
      {...(isBreakpointReached
        ? {
            className: 'bs-full overflow-y-auto overflow-x-hidden',
            onScroll: container => scrollMenu(container, false)
          }
        : {
            options: { wheelPropagation: false, suppressScrollX: true },
            onScrollY: container => scrollMenu(container, true)
          })}
    >
      <Menu
        menuItemStyles={menuItemStyles(theme)}
        renderExpandIcon={({ open }) => <RenderExpandIcon open={open} transitionDuration={transitionDuration} />}
        renderExpandedMenuItemIcon={{ icon: <i className='ri-circle-line' /> }}
        menuSectionStyles={menuSectionStyles(theme)}
      >
        {/* ── Overview ── */}
        <MenuItem href='/' icon={<i className='ri-dashboard-line' />}>
          Dashboard
        </MenuItem>

        {/* ── Operations ── */}
        <MenuSection label='Operations'>
          <MenuItem href='/users' icon={<i className='ri-group-line' />}>
            Users
          </MenuItem>
          <MenuItem href='/seasons' icon={<i className='ri-calendar-event-line' />}>
            Seasons
          </MenuItem>
          <MenuItem href='/season-points' icon={<i className='ri-trophy-line' />}>
            Season Points
          </MenuItem>
          <MenuItem href='/events' icon={<i className='ri-gamepad-line' />}>
            Game Events
          </MenuItem>
          <MenuItem href='/matches' icon={<i className='ri-sword-line' />}>
            Matches
          </MenuItem>
          <MenuItem href='/questions' icon={<i className='ri-question-answer-line' />}>
            Questions
          </MenuItem>
        </MenuSection>

        {/* ── Moderation & Safety ── */}
        <MenuSection label='Moderation'>
          <MenuItem href='/moderation' icon={<i className='ri-shield-check-line' />}>
            Player Moderation
          </MenuItem>
          <MenuItem href='/anticheat' icon={<i className='ri-bug-line' />}>
            Anti-Cheat
          </MenuItem>
          <MenuItem href='/escalations' icon={<i className='ri-alarm-warning-line' />}>
            Escalations
          </MenuItem>
        </MenuSection>

        {/* ── Economy ── */}
        <MenuSection label='Economy'>
          <MenuItem href='/economy' icon={<i className='ri-money-dollar-circle-line' />}>
            Economy
          </MenuItem>
          <MenuItem href='/player-transactions' icon={<i className='ri-exchange-funds-line' />}>
            Transactions
          </MenuItem>
          <MenuItem href='/rewards' icon={<i className='ri-gift-line' />}>
            Rewards
          </MenuItem>
          <MenuItem href='/powerups' icon={<i className='ri-flashlight-line' />}>
            Powerups
          </MenuItem>
        </MenuSection>

        {/* ── Communications ── */}
        <MenuSection label='Communications'>
          <MenuItem href='/notifications' icon={<i className='ri-notification-3-line' />}>
            Notifications
          </MenuItem>
        </MenuSection>

        {/* ── System ── */}
        <MenuSection label='System'>
          <MenuItem href='/config' icon={<i className='ri-settings-3-line' />}>
            Config & Flags
          </MenuItem>
          <MenuItem href='/security' icon={<i className='ri-lock-line' />}>
            Security Audit
          </MenuItem>
          <MenuItem href='/media' icon={<i className='ri-image-line' />}>
            Media
          </MenuItem>
          <MenuItem href='/observability' icon={<i className='ri-pulse-line' />}>
            Observability
          </MenuItem>
        </MenuSection>
      </Menu>
    </ScrollWrapper>
  )
}

export default VerticalMenu
