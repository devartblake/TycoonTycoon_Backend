/**
 * Button with scale animation feedback on click
 */

import { motion } from 'framer-motion';
import type { ButtonHTMLAttributes } from 'react';

interface AnimatedButtonProps extends Omit<ButtonHTMLAttributes<HTMLButtonElement>, 'type'> {
  children: React.ReactNode;
  variant?: 'primary' | 'secondary';
}

export function AnimatedButton({
  children,
  className = '',
  ...props
}: AnimatedButtonProps) {
  return (
    <motion.button
      whileHover={{ scale: 1.02 }}
      whileTap={{ scale: 0.98 }}
      transition={{ type: 'spring', stiffness: 400, damping: 10 }}
      className={className}
      {...(props as any)}
    >
      {children}
    </motion.button>
  );
}

export default AnimatedButton;
