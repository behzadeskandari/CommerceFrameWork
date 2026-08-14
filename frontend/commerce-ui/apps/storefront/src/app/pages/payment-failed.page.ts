import { Component, OnInit, inject } from '@angular/core';

import { ActivatedRoute, RouterLink } from '@angular/router';

import { TranslatePipe } from '@commerce/localization';



@Component({

  standalone: true,

  imports: [RouterLink, TranslatePipe],

  template: `

    <section class="payment-page" aria-labelledby="payment-failed-title">

      <h1 id="payment-failed-title">{{ 'payment.failed.title' | translate }}</h1>

      <p>{{ 'payment.failed.message' | translate }}</p>



      @if (orderNumber) {

        <p class="order-number">

          <strong>{{ 'orders.number' | translate }}:</strong> {{ orderNumber }}

        </p>

      }



      <div class="actions">

        <a routerLink="/checkout">{{ 'payment.failed.tryAgain' | translate }}</a>

        <a routerLink="/">{{ 'nav.home' | translate }}</a>

      </div>

    </section>

  `,

  styles: [`

    .payment-page { display: grid; gap: 1rem; max-width: 32rem; }

    .order-number { font-size: 1.125rem; }

    .actions { display: flex; gap: 1rem; flex-wrap: wrap; margin-top: 0.5rem; }

    .actions a { color: var(--primary, #0f766e); }

  `]

})

export class PaymentFailedPageComponent implements OnInit {

  private readonly route = inject(ActivatedRoute);



  orderNumber = '';



  ngOnInit(): void {

    this.orderNumber = this.route.snapshot.queryParamMap.get('orderNumber') ?? '';

  }

}


