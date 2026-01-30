import { forwardRef } from 'react';
import { cn } from '../../lib/utils';
import { motion, type HTMLMotionProps } from 'framer-motion';

interface CardProps extends HTMLMotionProps<'div'> {
    variant?: 'default' | 'flat';
}

const Card = forwardRef<HTMLDivElement, CardProps>(
    ({ className, variant = 'default', children, ...props }, ref) => {
        const variants = {
            default: 'bg-[var(--bg-main)] border border-[var(--border-subtle)] shadow-sm',
            flat: 'bg-[var(--bg-sidebar)] border border-transparent',
        };

        return (
            <motion.div
                ref={ref}
                className={cn(
                    'rounded-xl overflow-hidden transition-all duration-200',
                    variants[variant],
                    className
                )}
                {...props}
            >
                <div className="h-full">{children}</div>
            </motion.div>
        );
    }
);

Card.displayName = 'Card';

export { Card };
