import { formatPaise, paiseToRupees, rupeesToPaise } from './money';

describe('money', () => {
  it('formats paise as Indian rupees', () => {
    expect(formatPaise(12345678)).toBe('₹1,23,456.78');
  });

  it('rounds rupee input to whole paise', () => {
    expect(rupeesToPaise(10.005)).toBe(1001);
    expect(rupeesToPaise(0.1 + 0.2)).toBe(30);
  });

  it('converts paise back to rupees for form fields', () => {
    expect(paiseToRupees(11_050)).toBe(110.5);
  });
});
