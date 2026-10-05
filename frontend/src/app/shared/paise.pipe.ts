import { Pipe, PipeTransform } from '@angular/core';
import { formatPaise } from './money';

@Pipe({ name: 'paise' })
export class PaisePipe implements PipeTransform {
  transform(paise: number | null | undefined): string {
    return paise == null ? '' : formatPaise(paise);
  }
}
