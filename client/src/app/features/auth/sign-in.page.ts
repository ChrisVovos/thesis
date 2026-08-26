import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { ActivatedRoute, Router } from '@angular/router';
import { AuthStore } from '../../core/auth/auth.store';
import { isAppError, type AppError } from '../../core/errors/app-error';
import { TransportSelector } from '../../shared/components/transport-selector/transport-selector';

/** The sign-in screen. */
@Component({
  selector: 'app-sign-in-page',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    MatButtonModule,
    MatCardModule,
    MatFormFieldModule,
    MatIconModule,
    MatInputModule,
    MatProgressBarModule,
    ReactiveFormsModule,
    TransportSelector,
  ],
  template: `
    <div class="sign-in">
      <mat-card>
        <mat-card-header>
          <mat-card-title>Item Authoring</mat-card-title>
          <mat-card-subtitle>Sign in to continue</mat-card-subtitle>
        </mat-card-header>

        @if (auth.busy()) {
          <mat-progress-bar mode="indeterminate" aria-label="Signing in" />
        }

        <mat-card-content>
          <form [formGroup]="form" (ngSubmit)="submit()" novalidate>
            <mat-form-field appearance="outline">
              <mat-label>E-mail address</mat-label>
              <input
                matInput
                type="email"
                formControlName="email"
                autocomplete="username"
                data-testid="email"
              />
              @if (form.controls.email.touched && form.controls.email.hasError('required')) {
                <mat-error>An e-mail address is required.</mat-error>
              } @else if (form.controls.email.touched && form.controls.email.hasError('email')) {
                <mat-error>Enter a valid e-mail address.</mat-error>
              }
            </mat-form-field>

            <mat-form-field appearance="outline">
              <mat-label>Password</mat-label>
              <input
                matInput
                [type]="revealed() ? 'text' : 'password'"
                formControlName="password"
                autocomplete="current-password"
                data-testid="password"
              />
              <button
                matIconButton
                matSuffix
                type="button"
                [attr.aria-label]="revealed() ? 'Hide password' : 'Show password'"
                (click)="toggleReveal()"
              >
                <mat-icon>{{ revealed() ? 'visibility_off' : 'visibility' }}</mat-icon>
              </button>
              @if (form.controls.password.touched && form.controls.password.invalid) {
                <mat-error>A password is required.</mat-error>
              }
            </mat-form-field>

            @if (failure(); as error) {
              <p class="failure" role="alert" data-testid="sign-in-error">{{ error.message }}</p>
            }

            <button
              matButton="filled"
              type="submit"
              data-testid="sign-in"
              [disabled]="auth.busy()"
            >
              Sign in
            </button>
          </form>
        </mat-card-content>

        <mat-card-footer class="transport-slot">
          <app-transport-selector />
        </mat-card-footer>
      </mat-card>
    </div>
  `,
  styles: `
    .sign-in {
      display: flex;
      align-items: center;
      justify-content: center;
      min-height: 100vh;
      padding: 1rem;
      background: #f5f5f7;
    }

    mat-card { width: min(28rem, 100%); }
    form { display: flex; flex-direction: column; gap: 0.75rem; padding-top: 1rem; }
    .failure { color: var(--mat-sys-error, #b3261e); margin: 0; }
    .transport-slot { display: flex; justify-content: flex-end; padding: 0 1rem 1rem; }
  `,
})
export class SignInPage {
  private readonly formBuilder = inject(FormBuilder);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);

  /** The session store the form drives. */
  protected readonly auth = inject(AuthStore);

  /** The credentials form. */
  protected readonly form = this.formBuilder.nonNullable.group({
    email: ['', [Validators.required, Validators.email]],
    password: ['', [Validators.required]],
  });

  /** The failure from the last attempt, when it failed. */
  protected readonly failure = signal<AppError | null>(null);

  /** Whether the password is currently shown in clear text. */
  protected readonly revealed = signal(false);

  /** Toggles password visibility. */
  protected toggleReveal(): void {
    this.revealed.update((value) => !value);
  }

  /** Attempts a sign-in with the entered credentials. */
  protected async submit(): Promise<void> {
    this.failure.set(null);

    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    try {
      await this.auth.signIn(this.form.getRawValue());
      const returnUrl = this.route.snapshot.queryParamMap.get('returnUrl') ?? '/items';
      await this.router.navigateByUrl(returnUrl);
    } catch (error: unknown) {
      this.failure.set(
        isAppError(error)
          ? error
          : { code: 'auth.failed', message: 'Sign-in failed.', kind: 'failure' },
      );
    }
  }
}
