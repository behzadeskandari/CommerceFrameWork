import { Component, OnInit, inject } from '@angular/core';

import { ActivatedRoute, RouterLink } from '@angular/router';

import { TranslatePipe } from '@commerce/localization';



@Component({

  standalone: true,

  imports: [RouterLink, TranslatePipe],

  template: `

    <section class="payment-page" aria-labelledby="payment-processing-title">

      <h1 id="payment-processing-title">{{ 'payment.processing.title' | translate }}</h1>

      <p>{{ 'payment.processing.message' | translate }}</p>



      @if (orderNumber) {

        <p class="order-number">

          <strong>{{ 'orders.number' | translate }}:</strong> {{ orderNumber }}

        </p>

      }



      @if (instructions) {

        <div class="instructions" role="status">

          <h2>{{ 'payment.processing.instructions' | translate }}</h2>

          <p>{{ instructions }}</p>

        </div>

      }



      @if (redirectUrl) {

        <p>

          <a [href]="redirectUrl" class="btn">{{ 'payment.processing.continue' | translate }}</a>

        </p>

      }



      <div class="actions">

        <a routerLink="/">{{ 'nav.home' | translate }}</a>

      </div>

    </section>

  `,

  styles: [`

    .payment-page { display: grid; gap: 1rem; max-width: 32rem; }

    .order-number { font-size: 1.125rem; }

    .instructions { padding: 1rem; background: #fffbeb; border: 1px solid #fcd34d; border-radius: 0.5rem; }

    .instructions h2 { margin: 0 0 0.5rem; font-size: 1rem; }

    .btn {

      display: inline-block; padding: 0.625rem 1rem; background: var(--primary, #0f766e);

      color: #fff; text-decoration: none; border-radius: 0.375rem;

    }

    .actions { display: flex; gap: 1rem; flex-wrap: wrap; margin-top: 0.5rem; }

    .actions a { color: var(--primary, #0f766e); }

  `]

})

export class PaymentProcessingPageComponent implements OnInit {

  private readonly route = inject(ActivatedRoute);



  orderNumber = '';

  instructions = '';

  paymentId: number | null = null;

  redirectUrl = '';



  ngOnInit(): void {

    const params = this.route.snapshot.queryParamMap;

    this.orderNumber = params.get('orderNumber') ?? '';

    this.instructions = params.get('instructions') ?? '';

    const paymentIdParam = params.get('paymentId');

    this.paymentId = paymentIdParam ? Number(paymentIdParam) : null;

    this.redirectUrl = params.get('redirectUrl') ?? '';

  }

}


