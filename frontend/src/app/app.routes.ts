import { Routes } from '@angular/router';
import { signedInGuard } from './auth/signed-in.guard';

export const routes: Routes = [
  { path: '', pathMatch: 'full', redirectTo: 'auctions' },
  {
    path: 'sign-in',
    title: 'Sign in',
    loadComponent: () => import('./auth/sign-in-page').then((m) => m.SignInPage),
  },
  {
    path: 'auctions',
    title: 'Auctions',
    canActivate: [signedInGuard],
    loadComponent: () => import('./auctions/auction-list-page').then((m) => m.AuctionListPage),
  },
  {
    path: 'auctions/new',
    title: 'Sell an item',
    canActivate: [signedInGuard],
    loadComponent: () => import('./auctions/auction-form-page').then((m) => m.AuctionFormPage),
  },
  {
    path: 'auctions/:id',
    title: 'Auction',
    canActivate: [signedInGuard],
    loadComponent: () => import('./auctions/auction-page').then((m) => m.AuctionPage),
  },
  {
    path: 'auctions/:id/edit',
    title: 'Edit auction',
    canActivate: [signedInGuard],
    loadComponent: () => import('./auctions/auction-form-page').then((m) => m.AuctionFormPage),
  },
  {
    path: 'me',
    title: 'My activity',
    canActivate: [signedInGuard],
    loadComponent: () => import('./activity/my-activity-page').then((m) => m.MyActivityPage),
  },
  { path: '**', redirectTo: 'auctions' },
];
