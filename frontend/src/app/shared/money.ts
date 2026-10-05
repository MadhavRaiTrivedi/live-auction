const PAISE_PER_RUPEE = 100;

const rupeeFormat = new Intl.NumberFormat('en-IN', { style: 'currency', currency: 'INR' });

export function formatPaise(paise: number): string {
  return rupeeFormat.format(paise / PAISE_PER_RUPEE);
}

// Rounding happens once, at the edge, so the rest of the app only ever sees whole paise.
export function rupeesToPaise(rupees: number): number {
  return Math.round(rupees * PAISE_PER_RUPEE);
}

export function paiseToRupees(paise: number): number {
  return paise / PAISE_PER_RUPEE;
}
