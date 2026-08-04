import { Routes } from '@angular/router';
import { InboxComponent } from './features/inbox/inbox.component';

export const routes: Routes = [
  { path: '', component: InboxComponent },
  { path: 'inbox', component: InboxComponent },
];
